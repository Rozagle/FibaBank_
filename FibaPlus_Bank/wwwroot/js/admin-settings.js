function updateRate(input) {
    var id = input.getAttribute('data-id');
    var val = input.value;
    fetch('/Admin/UpdateInterestTier', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: 'id=' + id + '&rate=' + val
    }).then(res => res.json()).then(data => {
        if (data.success) {
            input.classList.add('bg-success', 'text-white', 'border-success');
            setTimeout(() => input.classList.remove('bg-success', 'text-white', 'border-success'), 1000);
        }
    });
}