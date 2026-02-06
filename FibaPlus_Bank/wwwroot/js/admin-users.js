
    function openAccountModal(id, name) {
        document.getElementById('accountUserId').value = id;
        document.getElementById('accountUserName').value = name;

        fetch('/Admin/GetUserAccounts?userId=' + id)
            .then(response => response.json())
            .then(data => {
                let select = document.getElementById('sourceAccountSelect');
                select.innerHTML = '<option value="">Para Yatırmadan Aç</option>';
                data.forEach(acc => {
                    select.innerHTML += `<option value="${acc.accountId}">${acc.accountName} - ${acc.accountNumber} (${acc.balance} ${acc.currencyCode})</option>`;
                });
            });

        new bootstrap.Modal(document.getElementById('modalAddAccount')).show();
    }

    function toggleVadeliOptions() {
        var isVadeli = document.getElementById('vadeli').checked;
        var div = document.getElementById('vadeliOptions');
        div.style.display = isVadeli ? 'block' : 'none';

        if(!isVadeli) {
            document.getElementById('initialAmount').value = "";
            document.getElementById('sourceAccountSelect').value = "";
        }
    }

    function openEditModal(id, name, email, identity, segment) {
        document.getElementById('editUserId').value = id;
        document.getElementById('editFullName').value = name;
        document.getElementById('editEmail').value = email;
        document.getElementById('editIdentity').value = identity || "";
        document.getElementById('editSegment').value = segment || "Standard";

        new bootstrap.Modal(document.getElementById('modalEditUser')).show();
    }

    function openCardModal(id, name) {
        document.getElementById('cardUserId').value = id;
        document.getElementById('cardUserName').value = name;
        new bootstrap.Modal(document.getElementById('modalAddCard')).show();
    }
