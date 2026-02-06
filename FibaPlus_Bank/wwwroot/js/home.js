document.addEventListener("DOMContentLoaded", function () {

    try {
        var dataPack = window.chartData;
        var chartCanvas = document.getElementById('assetsChart');

        if (chartCanvas && dataPack && dataPack.values && dataPack.values.length > 0) {

            var defaultColors = ['#6366f1', '#3b82f6', '#10b981', '#f59e0b', '#ef4444'];
            var useColors = (dataPack.colors && dataPack.colors.length > 0) ? dataPack.colors : defaultColors;

            const totalValue = dataPack.values.reduce((a, b) => a + b, 0);

            if (totalValue > 0) {
                new Chart(chartCanvas, {
                    type: 'doughnut',
                    data: {
                        labels: dataPack.labels,
                        datasets: [{
                            data: dataPack.values,
                            backgroundColor: useColors, 
                            borderWidth: 0,
                            hoverOffset: 5,
                            cutout: '75%'
                        }]
                    },
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: { display: false },
                            tooltip: {
                                callbacks: {
                                    label: function (context) {
                                        let label = context.label || '';
                                        let value = context.raw || 0;
                                        return label + ': ' + value.toLocaleString('tr-TR');
                                    }
                                }
                            }
                        }
                    }
                });
            }
        }
    } catch (err) {
        console.error("Grafik hatası:", err);
    }


    try {
        const cardInput = document.getElementById('dashboardCardInput');
        if (cardInput) {
            cardInput.addEventListener('input', function (e) {
                let value = e.target.value.replace(/\D/g, '');
                if (value.length > 16) value = value.substring(0, 16);
                let formattedValue = value.replace(/(\d{4})(?=\d)/g, '$1 ');
                e.target.value = formattedValue;
            });
        }
    } catch (err) {
        console.error("Kart input hatası:", err);
    }

    fetchRatesAndList();
});


function checkCardAndGo() {
    try {
        var input = document.getElementById('dashboardCardInput');
        var errorLabel = document.getElementById('cardError');
        var btn = input.closest('.dashboard-card').querySelector('button');

        errorLabel.style.display = 'none';
        errorLabel.innerText = "";

        var rawNumber = input.value.replace(/\s/g, '');

        if (rawNumber.length < 16) {
            errorLabel.innerText = "Lütfen 16 haneli kart numarasını girin.";
            errorLabel.style.display = 'block';
            return;
        }

        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Kontrol...';

        $.post('/Home/CheckCardStatus', { cardNumber: rawNumber }, function (response) {
            if (response.success) {
                window.location.href = '/Transfer/Index?prefillCard=' + rawNumber;
            } else {
                errorLabel.innerText = response.message;
                errorLabel.style.display = 'block';
                input.classList.add('is-invalid');
                setTimeout(() => input.classList.remove('is-invalid'), 2000);
            }
        }).fail(function () {
            errorLabel.innerText = "Sunucu bağlantı hatası.";
            errorLabel.style.display = 'block';
        }).always(function () {
            btn.disabled = false;
            btn.innerHTML = 'Devam Et <i class="fa-solid fa-arrow-right ms-2"></i>';
        });

    } catch (err) {
        console.error("Kart kontrol hatası:", err);
    }
}

let apiRates = {};
let currencyList = [];
let currentSide = 'from';
const currencyNames = new Intl.DisplayNames(['tr'], { type: 'currency' });

async function fetchRatesAndList() {
    try {
        const response = await fetch('https://open.er-api.com/v6/latest/USD');
        const data = await response.json();

        if (data.result === "success") {
            apiRates = data.rates;
           

            const topCurrencies = ['TRY', 'USD', 'EUR', 'GBP', 'XAU', 'JPY', 'CHF'];
            currencyList = Object.keys(apiRates).filter(code => !topCurrencies.includes(code)).sort();
            currencyList = [...topCurrencies, ...currencyList];

            populateModalList();
            calculateConversion(); 
        }
    } catch (error) {
        console.error("Döviz API hatası:", error);
        document.getElementById('conversionResult').innerText = "Hata";
    }
}

function populateModalList() {
    const container = document.getElementById('currencyListContainer');
    if (!container) return;

    container.innerHTML = "";
    currencyList.forEach(code => {
        let countryCode = code.slice(0, 2).toLowerCase();
        if (code === 'EUR') countryCode = 'eu';
        if (code === 'GBP') countryCode = 'gb';
        if (code === 'TRY') countryCode = 'tr';

        let flagUrl = `https://flagcdn.com/48x36/${countryCode}.png`;
        let currName = code;
        try { currName = currencyNames.of(code); } catch (e) { }

        if (code === 'XAU') {
            flagUrl = 'https://upload.wikimedia.org/wikipedia/commons/6/6d/Gold_Bullion_Icon.png';
            currName = "Gram Altın";
        }

        let item = `
            <button type="button" class="currency-item" onclick="selectCurrency('${code}', '${flagUrl}')">
                <img src="${flagUrl}" class="currency-flag me-3">
                <span class="currency-code me-3">${code}</span>
                <span class="currency-name">${currName}</span>
            </button>`;
        container.innerHTML += item;
    });
}

window.openCurrencyModal = function (side) {
    currentSide = side;
    var searchInput = document.getElementById('currencySearch');
    if (searchInput) searchInput.value = "";
    filterCurrencies();

    var el = document.getElementById('currencyModal');
    var modal = new bootstrap.Modal(el);
    modal.show();
}

window.selectCurrency = function (code, flagUrl) {
    if (currentSide === 'from') {
        document.getElementById('text-from').innerText = code;
        document.getElementById('img-from').src = flagUrl;
        document.getElementById('currFromValue').value = code;
    } else {
        document.getElementById('text-to').innerText = code;
        document.getElementById('img-to').src = flagUrl;
        document.getElementById('currToValue').value = code;
    }

    var el = document.getElementById('currencyModal');
    var modal = bootstrap.Modal.getInstance(el);
    if (modal) modal.hide();

    calculateConversion();
}

window.filterCurrencies = function () {
    let inputEl = document.getElementById('currencySearch');
    if (!inputEl) return;

    let input = inputEl.value.toLocaleLowerCase('tr');
    let items = document.getElementById('currencyListContainer').getElementsByClassName('currency-item');

    for (let item of items) {
        let text = item.innerText.toLocaleLowerCase('tr');
        item.style.display = text.indexOf(input) > -1 ? "" : "none";
    }
}

function calculateConversion() {
    const amountEl = document.getElementById('amountInput');
    const resultEl = document.getElementById('conversionResult');

    if (!amountEl || !resultEl) return;

    const amount = parseFloat(amountEl.value) || 0;
    const from = document.getElementById('currFromValue').value;
    const to = document.getElementById('currToValue').value;

    if (!apiRates[from] || !apiRates[to]) return;

    let rateFrom = apiRates[from];
    let rateTo = apiRates[to];

    let amountInUSD = amount / rateFrom;
    let result = amountInUSD * rateTo;
    let crossRate = rateTo / rateFrom;

    resultEl.innerText = result.toLocaleString('tr-TR', { maximumFractionDigits: 2 });

    if (document.getElementById('rateSource')) {
        document.getElementById('rateSource').innerText = from;
        document.getElementById('rateTarget').innerText = to;
        document.getElementById('rateValue').innerText = crossRate.toLocaleString('tr-TR', { maximumFractionDigits: 4 });
    }
}

window.swapCurrencies = function () {
    let fromCode = document.getElementById('currFromValue').value;
    let fromImg = document.getElementById('img-from').src;

    let toCode = document.getElementById('currToValue').value;
    let toImg = document.getElementById('img-to').src;

    document.getElementById('currFromValue').value = toCode;
    document.getElementById('text-from').innerText = toCode;
    document.getElementById('img-from').src = toImg;

    document.getElementById('currToValue').value = fromCode;
    document.getElementById('text-to').innerText = fromCode;
    document.getElementById('img-to').src = fromImg;

    calculateConversion();
}

const amtInput = document.getElementById('amountInput');
if (amtInput) {
    amtInput.addEventListener('input', calculateConversion);
}