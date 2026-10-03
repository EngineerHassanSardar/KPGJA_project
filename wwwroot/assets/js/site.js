document.addEventListener("DOMContentLoaded", function () {

    // Automatically update copyright year
    const yearElements = document.querySelectorAll("[data-year]");

    yearElements.forEach(function (element) {
        element.textContent = new Date().getFullYear();
    });

});