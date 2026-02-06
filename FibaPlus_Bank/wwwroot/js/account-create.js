
function updateUI() {
    var select = document.getElementById("productSelect");

    if (!select || select.selectedIndex === -1) return;

    var currency = select.options[select.selectedIndex].getAttribute("data-currency");
    var radioVadesiz = document.getElementById("typeVadesiz");
    var radioVadeli = document.getElementById("typeVadeli");
    var gridArea = $("#vadeliGridArea"); 
    var warning = $("#currencyWarning"); 

    if (currency !== "TRY") {
        radioVadesiz.checked = true;
        radioVadeli.disabled = true;
        warning.show();
        gridArea.slideUp();
    } else {
        radioVadeli.disabled = false;
        warning.hide();
        if (radioVadeli.checked) gridArea.slideDown();
        else gridArea.slideUp();
    }
}

$(document).ready(function () {
    updateUI();
});