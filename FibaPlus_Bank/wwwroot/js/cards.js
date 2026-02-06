
    function toggleCardStatus(cardId) {
        var btn = $('#btn-freeze-' + cardId);
    var visual = $('#card-visual-' + cardId);
    var lock = $('#lock-icon-' + cardId);

    btn.prop('disabled', true);

    $.post('/Cards/ToggleCardStatus', {cardId: cardId }, function (response) {
            if (response.success) {
                if (response.newStatus === "Inactive") {
     
        visual.addClass('frozen-effect'); 
    lock.fadeIn();
    btn.text("Aktif Et");
    btn.removeClass('btn-outline-danger').addClass('btn-success');

    Swal.fire({
        icon: 'info',
    title: 'Kart Donduruldu',
    text: 'Kartınız geçici olarak kullanıma kapatıldı.',
    timer: 1500,
    showConfirmButton: false
                    });
                } else {
      
        visual.removeClass('frozen-effect'); 
    lock.fadeOut(); 
    btn.text("Dondur");
    btn.removeClass('btn-success').addClass('btn-outline-danger');

    Swal.fire({
        icon: 'success',
    title: 'Kart Aktif',
    text: 'Kartınız tekrar kullanıma açıldı.',
    timer: 1500,
    showConfirmButton: false
                    });
                }
            } else {
        Swal.fire('Hata', response.message, 'error');
            }
    btn.prop('disabled', false);
        });
    }
