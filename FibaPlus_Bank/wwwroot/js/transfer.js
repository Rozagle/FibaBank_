$(document).ready(function () {

    $('#senderAccount').change(function () {
        var selected = $(this).find(':selected');
        if (selected.val() === "") return;
        var currency = selected.data('currency');
        var symbol = "₺"; var fee = "13.86 ₺";
        if (currency === "USD") { symbol = "$"; fee = "25.00 $"; }
        else if (currency === "EUR") { symbol = "€"; fee = "25.00 €"; }
        $('#currencySymbol').text(symbol);
        $('#feeWarning').text(fee);
    });

    $('#ibanInput').on('input', function () {
        var val = $(this).val().replace(/\s/g, ''); 
        var senderId = $('#senderAccount').val();

        if (val.length >= 20) { 
            $('#ibanError').hide();

            $.get('/Transfer/CheckIban', { iban: val, senderAccountId: senderId }, function (data) {
                if (data.isSameAccount) {
                    Swal.fire('Hata', data.message, 'warning');
                    $('#ibanInput').val('');
                    return;
                }
                if (data.success) {
                    $('#receiverName').val(data.name);
                    $('#bankLogo').html(`<i class="${data.logo} fs-5 text-primary"></i>`).show();

                    if (data.isLocked) {
                        $('#receiverName').prop('readonly', true).addClass('bg-light text-muted');
                    }
                } else {
                    $('#bankLogo').hide();
                    $('#receiverName').val('').prop('readonly', false).removeClass('bg-light text-muted');
                }
            });
        } else {
            $('#bankLogo').hide();
            if ($('#receiverName').prop('readonly')) {
                $('#receiverName').val('').prop('readonly', false).removeClass('bg-light text-muted');
            }
        }
    });

    window.confirmTransfer = function () {
        var senderSelect = document.getElementById('senderAccount');
        var selectedOption = senderSelect.options[senderSelect.selectedIndex];
        if (senderSelect.value === "") { Swal.fire('Uyarı', 'Hesap seçiniz.', 'warning'); return; }

        var accountType = selectedOption.getAttribute('data-type');
        var accountId = senderSelect.value;
        var rawAmount = $('input[name="Amount"]').val();
        var amount = parseFloat(rawAmount.replace(',', '.') || 0);

        if (!amount || amount <= 0) { Swal.fire('Uyarı', 'Geçersiz tutar.', 'warning'); return; }

        if (accountType === "Vadeli") {
            Swal.fire({
                title: '⚠️ Vade Gününüz Sıfırlanacak!',
                html: `<div class="text-start fs-6">
                        <p>Bu işlem <b>Vadeli Hesap</b> üzerinden yapılmaktadır.</p>
                        <ul class="text-danger small">
                            <li>Birikmiş faiz getirisini kaybedeceksiniz.</li>
                            <li>Vade süresi <b>bugün itibariyle sıfırlanacak</b>.</li>
                            <li>Hesabınız <b>Vadesiz Hesap</b> statüsüne dönecektir.</li>
                        </ul>
                        <p class="fw-bold mt-3 text-center">İşleme devam etmek istiyor musunuz?</p>
                       </div>`,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc3545',
                cancelButtonColor: '#6c757d',
                confirmButtonText: 'Evet, Vadeyi Boz ve Gönder',
                cancelButtonText: 'İptal Et'
            }).then((result) => {
                if (result.isConfirmed) {
                    var receiverIbanVal = $('#ibanInput').val();
                    $.post('/Transfer/BreakTerm', { accountId: accountId, receiverIban: receiverIbanVal }, function (res) {
                        if (res.success) {
                            $('#transferForm').submit();
                        } else {
                            Swal.fire('İşlem İptal Edildi', res.message, 'error');                        }
                    });
                }
            });
            return;
        }

        Swal.fire({
            title: 'Onaylıyor musunuz?',
            text: amount + " TL gönderilecek.",
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Gönder',
            confirmButtonColor: '#4361ee'
        }).then((result) => {
            if (result.isConfirmed) $('#transferForm').submit();
        });
    };

    window.downloadReceipt = function (ref, date, sender, receiver, amount) {
        Swal.fire({ title: 'Dekont', text: 'Hazırlanıyor...', icon: 'info', timer: 1000, showConfirmButton: false });
    };

});