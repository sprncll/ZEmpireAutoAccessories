// Unit picker on the "add line item" forms of Quotation, Job Order and
// Service Invoice.
//
// Paint Protection Film and Window Tint come off a roll and are written in
// cm, inches or metres; everything else is counted. Picking a product swaps
// the unit list to match, so nobody has to remember which products are
// measured. The server converts whatever unit is chosen into the unit stock
// is held in - this only decides what is offered.
(function () {
    var product = document.getElementById('addLineProduct');
    var unit = document.querySelector('select[name="unit"]');
    var quantity = document.querySelector('input[name="quantity"]');
    if (!product || !unit) { return; }

    var LENGTH_UNITS = ['cm', 'in', 'm'];
    var COUNT_UNITS = ['Unit', 'pc', 'set', 'roll'];

    function selectedIsRoll() {
        var option = product.options[product.selectedIndex];
        return !!option && option.dataset.roll === 'true';
    }

    function fill(values, preferred) {
        var previous = unit.value;
        unit.innerHTML = '';

        values.forEach(function (value) {
            var option = document.createElement('option');
            option.value = value;
            option.textContent = value;
            unit.appendChild(option);
        });

        // Keep what was already chosen when it is still on offer, so
        // re-picking the same kind of product doesn't reset it.
        unit.value = values.indexOf(previous) !== -1 ? previous : preferred;
    }

    function apply() {
        var roll = selectedIsRoll();
        fill(roll ? LENGTH_UNITS : COUNT_UNITS, roll ? 'cm' : 'Unit');

        // Quotation and Job Order lines store quantity as a whole number, so
        // a measured length has to be written in a unit that comes out whole
        // - centimetres. Leave the step alone where the column is decimal.
        if (quantity && quantity.step === '1') {
            quantity.title = roll
                ? 'Whole numbers only on this document - measure in cm for part metres'
                : '';
        }
    }

    // ---- stock ---------------------------------------------------------
    // What is on hand for the selected product, and whether the quantity
    // being written exceeds it. A job order or quotation line is a plan, and
    // stock only moves when the document is completed, so this warns rather
    // than blocks - the binding check runs server-side at that point.
    var note = document.getElementById('lineStockNote');

    function onHand() {
        var option = product.options[product.selectedIndex];
        if (!option || !option.value) { return null; }
        var stock = parseFloat(option.dataset.stock);
        return isNaN(stock) ? null : stock;
    }

    function describe(quantity, roll) {
        if (!roll) { return quantity.toLocaleString(undefined, { maximumFractionDigits: 2 }); }
        return quantity >= 100
            ? (quantity / 100).toFixed(2) + ' m'
            : quantity.toFixed(1) + ' cm';
    }

    function showStock() {
        if (!note) { return; }

        var stock = onHand();
        if (stock === null) { note.textContent = ''; note.className = 'form-text mt-1'; return; }

        var roll = selectedIsRoll();

        // Compare like with like: a line written in metres against stock held
        // in centimetres. Same factors as UnitOfMeasure on the server.
        var PER_BASE = { cm: 1, 'in': 2.54, m: 100 };
        var typed = quantity ? parseFloat(quantity.value) || 0 : 0;
        var wanted = roll ? typed * (PER_BASE[unit.value] || 1) : typed;

        if (stock <= 0) {
            note.textContent = 'Out of stock. This line can be written now, but the document cannot be completed until there is stock.';
            note.className = 'form-text mt-1 text-danger';
        } else if (wanted > stock) {
            note.textContent = 'Only ' + describe(stock, roll) + ' on hand - this line asks for ' + describe(wanted, roll) + '.';
            note.className = 'form-text mt-1 text-danger';
        } else {
            note.textContent = describe(stock, roll) + ' on hand.';
            note.className = 'form-text mt-1 text-muted';
        }
    }

    product.addEventListener('change', function () { apply(); showStock(); });
    unit.addEventListener('change', showStock);
    if (quantity) { quantity.addEventListener('input', showStock); }

    apply();
    showStock();
})();
