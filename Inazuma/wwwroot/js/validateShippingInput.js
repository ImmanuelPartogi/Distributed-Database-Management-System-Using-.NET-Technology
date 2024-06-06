function validateShippingInput() {
    if (document.getElementById("trackingNumber").value == "") {
        swal({
            icon: 'error',
            title: 'Oops....',
            text: 'Please enter the tracking Number'
        });
        return false;
    }

    if (document.getElementById("carrier").value == "") {
        swal({
            icon: 'error',
            title: 'Oops....',
            text: 'Please enter the carrier name'
        });
        return false;
    }

    return true; 
}


function EnterDataCarrier() {
    var carrierInput = document.getElementById("carrier");
    if (carrierInput.value.trim() === "") {
        carrierInput.style.border = "thin solid red";
        return false;
    } else {
        carrierInput.style.border = "thin solid #0000FF";
        return true;
    }
}

function EnterDataTracking() {
    var trackingInput = document.getElementById("trackingNumber");
    if (trackingInput.value.trim() === "") {
        trackingInput.style.border = "thin solid red";
        return false;
    } else {
        trackingInput.style.border = "thin solid #0000FF";
        return true;
    }
}

function RefundIssue(url) {
    swal({
        title: "Click OK if you want to refund the order",
        text: "If you click OK, it will not revert back again",
        icon: "warning",
        buttons: true,
        dangerMode: true
    }).then((willRefund) => {
        if (willRefund) {
            $.ajax({
                type: "POST",
                url: url,
                success: function (data) {
                    if (data.success) {
                        toastr.info(data.message);
                        setTimeout(reloadPage, 3000);
                    } else {
                        toastr.error(data.message);
                    }
                },
                error: function (xhr, status, error) {
                    toastr.error("An error occurred while processing the refund.");
                }
            });
        }
    });
}


function reloadPage() {
    location.reload(true);
}
