    function setTransactionType(type) {
        document.getElementById('txType').value = type;
    var btn = document.getElementById('actionBtn');
    var input = document.getElementById('symbolInput');

    if(input.value !== "") {
        btn.disabled = false;
    if (type === 'BUY') {
        btn.classList.remove('btn-danger');
    btn.classList.add('btn-primary');
    btn.innerHTML = '<i class="fa-solid fa-check me-2"></i>Alım İşlemini Onayla';
            } else {
        btn.classList.remove('btn-primary');
    btn.classList.add('btn-danger');
    btn.innerHTML = '<i class="fa-solid fa-check me-2"></i>Satış İşlemini Onayla';
            }
        }
    }

    function selectAsset(code, name, price, invType, instType) {
        document.getElementById('symbolInput').value = code;
    document.getElementById('invType').value = invType;
    document.getElementById('instType').value = instType;
    document.getElementById('instName').value = name;

    document.getElementById('priceInput').value = price.toString().replace('.', ',');

    var btn = document.getElementById('actionBtn');
    btn.disabled = false;
    var currentType = document.getElementById('txType').value;
    setTransactionType(currentType); 

    calculateTotal();

    var inputArea = document.getElementById('symbolInput');
    inputArea.style.backgroundColor = "#e0cffc";
        setTimeout(() => inputArea.style.backgroundColor = "#f8f9fa", 300);
    }


    function updateBalance() {
        var select = document.getElementById('accountSelect');
    if (select.selectedIndex === -1) return;

    var selectedOption = select.options[select.selectedIndex];
    var balance = parseFloat(selectedOption.getAttribute('data-balance').replace(',', '.')); 

    document.getElementById('balanceDisplay').innerHTML =
    '<i class="fa-solid fa-wallet me-1 text-muted"></i> ' +
    balance.toLocaleString('tr-TR', {minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ₺';
    }
    

    function calculateTotal() {
        var priceStr = document.getElementById('priceInput').value;
        var qtyStr = document.getElementById('qtyInput').value;

        var price = parseFloat(priceStr.replace(',', '.')) || 0;
        var qty = parseFloat(qtyStr) || 0;

        var total = price * qty;

        document.getElementById('totalPrice').innerText = total.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ₺';
    }


    document.addEventListener("DOMContentLoaded", function() {
        updateBalance();
    });

  