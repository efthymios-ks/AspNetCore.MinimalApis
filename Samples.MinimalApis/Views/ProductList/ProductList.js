document.addEventListener('DOMContentLoaded', () => {
    const rows = document.querySelectorAll('tbody tr');

    rows.forEach(row => {
        row.addEventListener('click', () => {
            const name = row.querySelector('td:last-child').textContent;
            console.log(`Selected product: ${name}`);
        });
    });
});
