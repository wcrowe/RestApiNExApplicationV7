
$(document).ready(function () {

    $("#namexxxxxxxxxxxxxt").on("click",
        function () {
        });

    });

function setEdit(viewid, editid) {
    resetTableView();
    $("#" + viewid).hide();
    $("#" + editid).show();
};

function showSaveCancel(id) {
        $("#save-" + id).show();
        $("#cancel-" + id).show();
};

function hideSaveCancel(id) {
    resetTableView();
    $("#save-" + id).hide();
    $("#cancel-" + id).hide();
        //$("input[id$=-" + id + "]").hide;
};

function resetTableView() {
    $(".editdata").hide();
    $(".viewdata").show();
}