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
    const message = sample.postData();
    hubConnection.invoke("Send", message);
});

$("#stopParsing").on("click", function (e) {
    const successCount = $('#logger > .product').length;
    const errorCount = $('#logger > .product.error').length;
    $('.result').append('<p><u>Количество спарсенных ' + (successCount - errorCount) + ', ошибок ' + errorCount + '</u></p>');
    hubConnection.stop();
});

hubConnection.start().catch(err => console.error(err.toString()));

const sample = {};
sample.postData = function () {
    const url = $('#homeUrl').val();
    const productList = $('#productList').val();
    const name = $('#name').val();
    const description = $('#description').val();
    const image = $('#image').val();
    const price = $('#price').val();

    $.ajax({
        type: "POST",
        url: "/Home/StartParcing",
        data: {
            "homeUrl": url,
            "productList": productList,
            "name": name,
            "description": description,
            "image": image,
            "price": price
        },
        accept: 'application/json',
        success: function (data) {  }
    });
};