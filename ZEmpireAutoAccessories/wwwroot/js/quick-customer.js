// Adding a customer without leaving the screen that needs one.
//
// The panel sits on the sale and on the quotation. Both are a form with a
// customer dropdown at the top of it, and on both, walking off to the
// Customers module to add somebody loses whatever has been typed.
//
// Posted with fetch rather than by submitting, because the panel sits
// INSIDE that form and a form cannot hold another: the button would send
// the sale instead of the customer.
//
// The markup is the same on both screens, so this is one file rather than
// two scripts drifting apart. The screen says where to post through
// data-endpoint on the toggle button; everything else is by id.
(function () {
    var toggle = document.getElementById('newCustomerToggle');
    var panel = document.getElementById('newCustomerPanel');
    var select = document.getElementById(
        toggle && toggle.getAttribute('data-customer-select') || 'customerId');

    if (!toggle || !panel || !select) { return; }

    var endpoint = toggle.getAttribute('data-endpoint');
    if (!endpoint) { return; }

    var error = document.getElementById('newCustomerError');
    var last = document.getElementById('newCustomerLast');
    var first = document.getElementById('newCustomerFirst');
    var number = document.getElementById('newCustomerNumber');
    var save = document.getElementById('newCustomerSave');
    var cancel = document.getElementById('newCustomerCancel');

    function show(message) {
        error.textContent = message || '';
        error.style.display = message ? '' : 'none';
    }

    function open(yes) {
        panel.style.display = yes ? '' : 'none';
        show('');
        if (yes) { last.focus(); }
    }

    toggle.addEventListener('click', function () {
        open(panel.style.display === 'none');
    });

    if (cancel) { cancel.addEventListener('click', function () { open(false); }); }

    // The number is stored as +63 and ten digits, so the box beside the +63
    // holds the ten: whatever is pasted is reduced to them.
    function tenDigits() {
        setTimeout(function () {
            var digits = number.value.replace(/\D/g, '');

            if (digits.length === 12 && digits.indexOf('63') === 0) { digits = digits.slice(2); }
            else if (digits.length === 11 && digits.charAt(0) === '0') { digits = digits.slice(1); }

            digits = digits.slice(0, 10);
            if (digits !== number.value) { number.value = digits; }
        }, 0);
    }

    number.addEventListener('input', tenDigits);
    number.addEventListener('paste', tenDigits);

    // Enter inside the panel adds the customer. Without this it submits the
    // form the panel is sitting in, which is the sale.
    panel.addEventListener('keydown', function (e) {
        if (e.key !== 'Enter') { return; }
        e.preventDefault();
        save.click();
    });

    save.addEventListener('click', function () {
        show('');
        save.disabled = true;

        var form = toggle.closest('form');
        var token = form && form.querySelector('input[name="__RequestVerificationToken"]');

        var body = new URLSearchParams();
        body.set('lastName', last.value);
        body.set('firstName', first.value);
        body.set('contactNumber', number.value);
        if (token) { body.set('__RequestVerificationToken', token.value); }

        fetch(endpoint, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: body.toString(),
            credentials: 'same-origin'
        }).then(function (response) {
            if (!response.ok) { throw new Error(response.status); }
            return response.json();
        }).then(function (result) {
            if (result.error) { show(result.error); return; }

            // Only ever a customer that did not exist a moment ago - one
            // already on file is refused rather than resolved - so the option
            // is always new. Guarded anyway: a second option for one person
            // would be worse than the line it saves.
            if (!select.querySelector('option[value="' + result.id + '"]')) {
                var option = document.createElement('option');
                option.value = result.id;
                option.textContent = result.name;
                select.appendChild(option);
            }

            select.value = String(result.id);

            // Fired so the screen reacts exactly as it would had the customer
            // been picked by hand - the vehicle list reloads for them.
            select.dispatchEvent(new Event('change', { bubbles: true }));

            open(false);
            last.value = '';
            first.value = '';
            number.value = '';
        }).catch(function () {
            show('The customer could not be added. Check the connection and try again.');
        }).then(function () {
            save.disabled = false;
        });
    })
})();
