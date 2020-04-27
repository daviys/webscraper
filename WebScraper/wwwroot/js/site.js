// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
const hubConnection = new signalR.HubConnectionBuilder()
    .withUrl("/scraper")
    .build();

hubConnection.on("Send", function (data) {
    $('#logger').append('<div class="product">' + data + '</div>');
});

$("#startParsing").on("click", function (e) {
    let url = $('#homeUrl').val();
    let message = $.get("/Home/StartParcing?homeUrl=" + url);
    hubConnection.invoke("Send", message);
});

hubConnection.start().catch(err => console.error(err.toString()));