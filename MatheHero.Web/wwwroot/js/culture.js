window.setCulture = function (culture) {
    document.cookie = ".AspNetCore.Culture=c=" + culture + "|uic=" + culture + "; path=/";
    location.reload();
}