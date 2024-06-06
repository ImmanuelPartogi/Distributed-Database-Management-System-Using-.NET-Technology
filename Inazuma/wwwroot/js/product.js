function Delete(url) {
    swal({
        title: "Are you sure?",
        text: "If you click OK, it will be permanently deleted.",
        icon: "warning",
        buttons: true,
        dangerMode: true
    }).then((willDelete) => {
        if (willDelete) {
            $.ajax({
                type: "DELETE",
                url: url,
                success: function (data) {
                    if (data.success) {
                        toastr.success(data.message);
                        setTimeout(reloadPage(), 3000);
                    } else {
                        toastr.error(data.message);
                    }
                },
                error: function (xhr, status, error) {
                    toastr.error("An error occurred while deleting the item.");
                }
            });
        }
    });
}

function reloadPage() {
    window.location.reload();
}


// JavaScript untuk menghitung Total Order Amount
function calculateTotalOrderAmount() {
    let totalOrderAmount = 0;
    // Loop melalui setiap item di keranjang belanja dan tambahkan ke totalOrderAmount
    $(".item-row").each(function () {
        let quantity = parseInt($(this).find(".quantity-input").val());
        let price = parseFloat($(this).find(".price-input").val());
        totalOrderAmount += quantity * price;
    });
    // Update nilai input Total Order Amount
    $("#totalOrderAmount").val(totalOrderAmount.toFixed(2));
}

// Event listener untuk menghitung ulang saat nilai input berubah
$(".quantity-input, .price-input").on("input", function () {
    calculateTotalOrderAmount();
});
