$(function () {

    var token = $.cookie("Token");
    var areaName = "StockpileManagement";
    var entityName = "StockpileState";
    var url = "/api/" + areaName + "/" + entityName;

    function formatTanggal(x) {
        theDate = new Date(x);
        formatted_date = theDate.getFullYear() + "-" + (theDate.getMonth() + 1).toString().padStart(2, "0")
            + "-" + theDate.getDate().toString().padStart(2, "0") + " " + theDate.getHours().toString().padStart(2, "0")
            + ":" + theDate.getMinutes().toString().padStart(2, "0");
        return formatted_date;
    }

    var tgl1 = sessionStorage.getItem("stockpilestateDate1");
    var tgl2 = sessionStorage.getItem("stockpilestateDate2");

    var date = new Date(), y = date.getFullYear(), m = date.getMonth();
    var firstDay = new Date(y, m, 1);
    var lastDay = new Date(y, m + 1, 0);

    if (tgl1 != null)
        firstDay = Date.parse(tgl1);

    if (tgl2 != null)
        lastDay = Date.parse(tgl2);

    $("#date-box1").dxDateBox({
        type: "datetime",
        displayFormat: 'dd MMM yyyy HH:mm',
        value: firstDay,
        onValueChanged: function (data) {
            firstDay = new Date(data.value);
            sessionStorage.setItem("stockpilestateDate1", formatTanggal(firstDay));
            _loadUrl = url + "/DataGrid/" + encodeURIComponent(formatTanggal(firstDay))
                + "/" + encodeURIComponent(formatTanggal(lastDay));
        }
    });

    $("#date-box2").dxDateBox({
        type: "datetime",
        displayFormat: 'dd MMM yyyy HH:mm',
        value: lastDay,
        onValueChanged: function (data) {
            lastDay = new Date(data.value);
            sessionStorage.setItem("stockpilestateDate2", formatTanggal(lastDay));
            _loadUrl = url + "/DataGrid/" + encodeURIComponent(formatTanggal(firstDay))
                + "/" + encodeURIComponent(formatTanggal(lastDay));
        }
    });

    $('#btnView').on('click', function () {
        location.reload();
    })

    var _loadUrl = url + "/DataGrid/" + encodeURIComponent(formatTanggal(firstDay))
        + "/" + encodeURIComponent(formatTanggal(lastDay));

    $("#grid").dxDataGrid({
        dataSource: DevExpress.data.AspNet.createStore({
            key: "id",
            //loadUrl: url + "/DataGrid",
            loadUrl: _loadUrl,
            onBeforeSend: function (method, ajaxOptions) {
                ajaxOptions.xhrFields = { withCredentials: true };
                ajaxOptions.beforeSend = function (request) {
                    request.setRequestHeader("Authorization", "Bearer " + token);
                };
            }
        }),
        remoteOperations: false,
        allowColumnResizing: true,
        columnResizingMode: "widget",
        columns: [
            {
                dataField: "stock_location_name",
                dataType: "string",
                caption: "Stockpile",
                formItem: {
                    colSpan: 2
                },
                groupIndex: 0,
                allowEditing: false,
                sortOrder: "asc"
            },
            {
                dataField: "transaction_datetime",
                dataType: "datetime",
                caption: "Transaction Datetime",
                allowEditing: false,
                sortIndex: 1,
                sortOrder: "desc"
            },
            {
                dataField: "qty_opening",
                dataType: "number",
                caption: "Opening",
                allowEditing: false
            },
            {
                dataField: "qty_in",
                dataType: "number",
                caption: "In",
                allowEditing: false
            },
            {
                dataField: "qty_out",
                dataType: "number",
                caption: "Out",
                allowEditing: false
            },
            {
                dataField: "qty_adjustment",
                dataType: "number",
                caption: "Adjustment",
                allowEditing: false
            },
            {
                dataField: "qty_closing",
                dataType: "number",
                caption: "Closing",
                allowEditing: false
            }
        ],
        filterRow: {
            visible: true
        },
        headerFilter: {
            visible: true
        },
        groupPanel: {
            visible: true
        },
        searchPanel: {
            visible: true,
            width: 240,
            placeholder: "Search..."
        },
        filterPanel: {
            visible: true
        },
        filterBuilderPopup: {
            position: { of: window, at: "top", my: "top", offset: { y: 10 } },
        },
        columnChooser: {
            enabled: true,
            mode: "select"
        },
        paging: {
            pageSize: 10
        },
        pager: {
            allowedPageSizes: [10, 20, 50, 100],
            showNavigationButtons: true,
            showPageSizeSelector: true,
            showInfo: true,
            visible: true
        },
        height: 600,
        showBorders: true,
        editing: {
            mode: "form",
            allowAdding: false,
            allowUpdating: false,
            allowDeleting: false,
            useIcons: true
        },
        grouping: {
            contextMenuEnabled: true,
            autoExpandAll: false
        },
        rowAlternationEnabled: true,
        export: {
            enabled: true,
            allowExportSelectedData: true
        },
        onExporting: function (e) {
            var workbook = new ExcelJS.Workbook();
            var worksheet = workbook.addWorksheet(entityName);

            DevExpress.excelExporter.exportDataGrid({
                component: e.component,
                worksheet: worksheet,
                autoFilterEnabled: true
            }).then(function () {
                // https://github.com/exceljs/exceljs#writing-xlsx
                workbook.xlsx.writeBuffer().then(function (buffer) {
                    saveAs(new Blob([buffer], { type: 'application/octet-stream' }), entityName + '.xlsx');
                });
            });
            e.cancel = true;
        }
    });

    $('#btnUpload').on('click', function () {
        $("#upload-indicator").attr("hidden", false);
        $("#upload-result").attr("hidden", false);

        var f = $("#fUpload")[0].files;
        console.log(f.length);
        var filename = $('#fUpload').val();

        if (f.length == 0) {
            alert("Please select a file.");
            return false;
        }
        else {
            var fileExtension = ['xlsx', 'xlsm', 'xls'];
            var extension = filename.replace(/^.*\./, '');
            if ($.inArray(extension, fileExtension) == -1) {
                alert("Please select only Excel files.");
                return false;
            }
        }

        var reader = new FileReader();
        reader.readAsDataURL(f[0]);
        reader.onload = function () {
            var formData = {
                "filename": f[0].name,
                "filesize": f[0].size,
                "data": reader.result.split(',')[1]
            };
            $.ajax({
                url: "/api/StockpileManagement/StockpileState/UploadDocument",
                type: 'POST',
                cache: false,
                contentType: "application/json",
                data: JSON.stringify(formData),
                headers: {
                    "Authorization": "Bearer " + token
                }
            }).done(function (result) {
                alert(result);
                location.reload();

            }).fail(function (jqXHR, textStatus, errorThrown) {
                window.location = '/General/General/UploadError';
                alert('File gagal di-upload!');
            });
        };
        reader.onerror = function (error) {
            console.log('Error: ', error);
        };
    });

});