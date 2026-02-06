function downloadPDF() {
    const element = document.getElementById('pdf-template');
    const opt = { margin: 0.3, filename: 'FibraBank_Dekont.pdf', image: { type: 'jpeg', quality: 1 }, html2canvas: { scale: 3 }, jsPDF: { unit: 'in', format: 'a4', orientation: 'portrait' } };
    Swal.fire({ title: 'Hazırlanıyor...', html: 'Dekontunuz oluşturuluyor.', didOpen: () => Swal.showLoading() });
    html2pdf().set(opt).from(element).save().then(() => Swal.close());
}