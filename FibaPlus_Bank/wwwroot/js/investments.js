document.addEventListener("DOMContentLoaded", function () {

    var dataPack = window.chartData;

    if (!dataPack || !dataPack.values || dataPack.values.length === 0) {
        var chartCanvas = document.getElementById('portfolioChart');
        if (chartCanvas) {
            var container = chartCanvas.parentElement;
            container.innerHTML = '<div class="d-flex flex-column align-items-center justify-content-center h-100 text-muted"><i class="fa-solid fa-chart-pie fa-2x mb-2 opacity-50"></i><small>Grafik verisi yok</small></div>';
        }
        return;
    }

    var rawLabels = dataPack.labels;
    var rawData = dataPack.values;
    var rawColors = dataPack.colors;

    console.log("Grafik Verileri Yüklendi:", rawData);

    var chartCanvas = document.getElementById('portfolioChart');

    if (chartCanvas) {
        new Chart(chartCanvas, {
            type: 'doughnut',
            data: {
                labels: rawLabels,
                datasets: [{
                    data: rawData,
                    backgroundColor: rawColors,
                    borderWidth: 0,
                    hoverOffset: 15
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            usePointStyle: true,
                            padding: 20,
                            font: {
                                family: "'Inter', sans-serif",
                                size: 12
                            }
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                let label = context.label || '';
                                let value = context.raw || 0;
                                return label + ': ' + new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(value);
                            }
                        }
                    }
                },
                cutout: '70%',
                layout: {
                    padding: 20
                }
            }
        });
    }
});