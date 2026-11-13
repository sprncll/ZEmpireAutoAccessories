// Unit picker on the "add line item" forms of Quotation, Job Order and
// Service Invoice.
//
// Three kinds of thing go on a line and each is written differently. Paint
// Protection Film and Window Tint come off a roll, so they are measured in
// cm, inches or metres. A horn or a step board is counted, in pieces, pairs
// or sets. Labour is neither - it is one job.
//
// Picking a product or a service swaps the unit list to match, so nobody has
// to remember which is which, and no unit is offered where it says nothing.
// The server converts whatever unit is chosen into the unit stock is held in
// - this only decides what is offered.
(function () {
    var product = document.getElementById('addLineProduct');
    var service = document.getElementById('addLineService');
    var unit = document.querySelector('select[name="unit"]');
    var quantity = document.querySelector('input[name="quantity"]');
    if (!product || !unit) { return; }

    // Both lists come from UnitOfMeasure on the server, rendered onto the
    // select, so this file cannot drift from what the rest of the system
    // treats as a valid unit.
    function unitList(attribute, fallback) {
        var raw = unit.dataset[attribute];
        return raw ? raw.split(',').map(function (u) { return u.trim(); }).filter(Boolean) : fallback;
    }

    // Shown in place of the dropdown when there is only one unit to have.
    var fixedUnit = document.createElement('span');
    fixedUnit.className = 'input-group-text line-unit-fixed';
    fixedUnit.style.display = 'none';
    unit.parentNode.insertBefore(fixedUnit, unit.nextSibling);

    var LENGTH_UNITS = unitList('lengthUnits', ['cm', 'in', 'm']);
    var COUNT_UNITS = unitList('countUnits', ['pc']);
    var SERVICE_UNITS = unitList('serviceUnits', ['job']);

    function selectedIsRoll() {
        var option = product.options[product.selectedIndex];
        return !!option && option.dataset.roll === 'true';
    }

    function selectedIsService() {
        return !!service && !!service.value;
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
        // Each unit only where it means something: film is measured, goods
        // are counted, work is a job. Offering all four against everything is
        // what made "pair" and "job" look arbitrary on a line of film.
        var roll = selectedIsRoll();
        var units = selectedIsService() ? SERVICE_UNITS
                  : roll ? LENGTH_UNITS
                  : COUNT_UNITS;

        fill(units, units[0]);

        // One unit is not a choice: say it beside the number instead of
        // offering a dropdown with a single entry. The select stays in the
        // form, out of sight, so the unit is still sent.
        var single = units.length === 1;
        unit.style.display = single ? 'none' : '';
        fixedUnit.textContent = single ? units[0] : '';
        fixedUnit.style.display = single ? '' : 'none';

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
    if (service) { service.addEventListener('change', function () { apply(); showStock(); }); }
    unit.addEventListener('change', showStock);
    if (quantity) { quantity.addEventListener('input', showStock); }

    apply();
    showStock();
})();
