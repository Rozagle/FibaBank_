function downloadReceipt(ref) {
    Swal.fire({
        title: 'Dekont İndiriliyor',
        text: ref + ' referans numaralı işlem hazırlanıyor...',
        icon: 'info',
        timer: 1500,
        showConfirmButton: false
    });
}