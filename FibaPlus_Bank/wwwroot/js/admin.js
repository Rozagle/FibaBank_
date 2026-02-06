
document.addEventListener("DOMContentLoaded", function () {

    const waveEl = document.getElementById('mainWaveChart');
    if (waveEl) {
        const ctxWave = waveEl.getContext('2d');

        window.waveChart = new Chart(ctxWave, {
            type: 'line',
            data: {
                labels: [],
                datasets: [
                    {
                        label: 'Gelir',
                        data: [],
                        borderColor: '#10b981',
                        backgroundColor: 'rgba(16, 185, 129, 0.05)',
                        borderWidth: 3,
                        tension: 0.4,
                        fill: true,
                        pointRadius: 0,
                        pointHoverRadius: 6
                    },
                    {
                        label: 'Gider',
                        data: [],
                        borderColor: '#f59e0b',
                        backgroundColor: 'transparent',
                        borderWidth: 3,
                        borderDash: [5, 5],
                        tension: 0.4,
                        pointRadius: 0
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { position: 'top', align: 'end' } },
                scales: {
                    y: { grid: { borderDash: [5, 5] }, beginAtZero: true },
                    x: { grid: { display: false } }
                }
            }
        });

        updateChart('weekly');
    }
});

function updateChart(period) {
    fetch(`/Admin/GetChartData?period=${period}`)
        .then(response => response.json())
        .then(data => {
            if (window.waveChart) {
                window.waveChart.data.labels = data.labels;
                window.waveChart.data.datasets[0].data = data.income;
                window.waveChart.data.datasets[1].data = data.expense;
                window.waveChart.update();
            }
        })
        .catch(err => console.error("Grafik verisi yüklenemedi:", err));
}

function initPieChart(dataValues) {
    const pieEl = document.getElementById('pieChart');
    if (pieEl) {
        const hasData = dataValues && dataValues.some(x => x > 0);
        const finalData = hasData ? dataValues : [1, 1, 1];
        const bgColors = hasData ? ['#6366f1', '#10b981', '#f59e0b'] : ['#e2e8f0', '#e2e8f0', '#e2e8f0'];

        new Chart(pieEl.getContext('2d'), {
            type: 'doughnut',
            data: {
                labels: ['TRY', 'USD', 'EUR'],
                datasets: [{
                    data: finalData,
                    backgroundColor: bgColors,
                    borderWidth: 0,
                    hoverOffset: 5
                }]
            },
            options: {
                cutout: '75%',
                plugins: { legend: { display: false } }
            }
        });
    }
}