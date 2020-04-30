// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
const connectionLastWord = 'Количество';
const errorMsg = 'Error';
const hubConnection = new signalR.HubConnectionBuilder()
    .withUrl("/scraper")
    .build();

hubConnection.on("Send", function (data) {
    if (data.indexOf(connectionLastWord) > -1) {
        $('.result').prepend('<p><u>' + data + '</u></p>');
        hubConnection.stop();
        return;
    }

    if (data.indexOf(errorMsg) < 0) {
        $('#logger').append('<div class="product">' + data + '</div>');
    } else {
        $('#logger').append('<div class="product error">' + data + '</div>');
    }
});

$("#startParsing").on("click", function (e) {
    let message = sample.postData();
    hubConnection.invoke("Send", message);
});

$("#stopParsing").on("click", function (e) {
    let successCount = $('#logger > .product').length;
    let errorCount = $('#logger > .product.error').length;
    $('.result').append('<p><u>Количество спарсенных ' + (successCount - errorCount) + ', ошибок ' + errorCount + '</u></p>');
    hubConnection.stop();
});

hubConnection.start().catch(err => console.error(err.toString()));

var sample = {};
sample.postData = function () {
    let url = $('#homeUrl').val();
    let productList = $('#productList').val();
    let name = $('#name').val();
    let description = $('#description').val();
    let image = $('#image').val();
    let price = $('#price').val();

    $.ajax({
        type: "POST",
        url: "/Home/StartParcing",
        data: { "homeUrl": url, "productList": productList, "name": name, "description": description, "image": image, "price": price },
        accept: 'application/json',
        success: function (data) {  }
    });
};