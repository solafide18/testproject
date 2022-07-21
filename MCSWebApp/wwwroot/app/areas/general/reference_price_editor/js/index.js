$(function () {

    var token = $.cookie("Token");
    var areaName = "General";
    var entityName = "ReferencePriceEditor";
    var url = "/api/" + areaName + "/" + entityName;

    $("#grid").dxDataGrid({
        dataSource: DevExpress.data.AspNet.createStore({
            key: "id",
            loadUrl: url + "/DataGrid",
            insertUrl: url + "/InsertData",
            updateUrl: url + "/UpdateData",
            deleteUrl: url + "/DeleteData",
            onBeforeSend: function (method, ajaxOptions) {
                ajaxOptions.xhrFields = { withCredentials: true };
                ajaxOptions.beforeSend = function (request) {
                    request.setRequestHeader("Authorization", "Bearer " + token);
                };
            }
        }),
        remoteOperations: true,
        allowColumnResizing: true,
        columnResizingMode: "widget",
        columns: [
            {
                dataField: "price_name",
                dataType: "string",
                caption: "Price Name",
                validationRules: [{
                    type: "required",
                    message: "This field is required."
                }],
                formItem: {
                    colSpan: 2
                }
            },
            {
                dataField: "start_date",
                dataType: "date",
                caption: "Start Date",
                validationRules: [{
                    type: "required",
                    message: "This field is required."
                }]
            },
            {
                dataField: "end_date",
                dataType: "date",
                caption: "End Date",
                validationRules: [{
                    type: "required",
                    message: "This field is required."
                }]
            },
            {
                dataField: "price",
                dataType: "number",
                caption: "Price",
                format: "fixedPoint",
                formItem: {
                    colSpan: 2,
                    editorType: "dxNumberBox",
                    editorOptions: {
                        format: "fixedPoint",
                    }
                },
            },
            {
                dataField: "currency_id",
                dataType: "text",
                caption: "Currency",
                visible: false,
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: "/api/General/Currency/CurrencyIdLookup",
                        onBeforeSend: function (method, ajaxOptions) {
                            ajaxOptions.xhrFields = { withCredentials: true };
                            ajaxOptions.beforeSend = function (request) {
                                request.setRequestHeader("Authorization", "Bearer " + token);
                            };
                        }
                    }),
                    valueExpr: "value",
                    displayExpr: "text"
                },
            },
            {
                dataField: "currency_uom_id",
                dataType: "text",
                caption: "Currency Unit",
                visible: false,
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: "/api/UOM/UOM/UomIdLookup",
                        onBeforeSend: function (method, ajaxOptions) {
                            ajaxOptions.xhrFields = { withCredentials: true };
                            ajaxOptions.beforeSend = function (request) {
                                request.setRequestHeader("Authorization", "Bearer " + token);
                            };
                        }
                    }),
                    valueExpr: "value",
                    displayExpr: "text"
                },
            },
            {
                dataField: "calori",
                dataType: "number",
                caption: "Calorie",
                format: "fixedPoint",
                formItem: {
                    editorType: "dxNumberBox",
                    editorOptions: {
                        format: "fixedPoint",
                    }
                },
            },
            {
                dataField: "calori_uom_id",
                dataType: "text",
                caption: "Calorie Unit",
                visible: false,
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: "/api/UOM/UOM/UomIdLookup",
                        onBeforeSend: function (method, ajaxOptions) {
                            ajaxOptions.xhrFields = { withCredentials: true };
                            ajaxOptions.beforeSend = function (request) {
                                request.setRequestHeader("Authorization", "Bearer " + token);
                            };
                        }
                    }),
                    valueExpr: "value",
                    displayExpr: "text"
                },
            },
            {
                dataField: "total_moisture",
                dataType: "number",
                caption: "Total Moisture",
                format: "fixedPoint",
                formItem: {
                    editorType: "dxNumberBox",
                    editorOptions: {
                        format: "fixedPoint",
                    }
                },
            },
            {
                dataField: "total_moisture_uom_id",
                dataType: "text",
                caption: "Moisture Unit",
                visible: false,
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: "/api/UOM/UOM/UomIdLookup",
                        onBeforeSend: function (method, ajaxOptions) {
                            ajaxOptions.xhrFields = { withCredentials: true };
                            ajaxOptions.beforeSend = function (request) {
                                request.setRequestHeader("Authorization", "Bearer " + token);
                            };
                        }
                    }),
                    valueExpr: "value",
                    displayExpr: "text"
                },
            },
            {
                dataField: "total_sulphur",
                dataType: "number",
                caption: "Total Sulphur",
                format: "fixedPoint",
                formItem: {
                    editorType: "dxNumberBox",
                    editorOptions: {
                        format: "fixedPoint",
                    }
                },
            },
            {
                dataField: "total_sulphur_uom_id",
                dataType: "text",
                caption: "Sulphur Unit",
                visible: false,
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: "/api/UOM/UOM/UomIdLookup",
                        onBeforeSend: function (method, ajaxOptions) {
                            ajaxOptions.xhrFields = { withCredentials: true };
                            ajaxOptions.beforeSend = function (request) {
                                request.setRequestHeader("Authorization", "Bearer " + token);
                            };
                        }
                    }),
                    valueExpr: "value",
                    displayExpr: "text"
                },
            },
            {
                dataField: "ash",
                dataType: "number",
                caption: "Ash",
                format: "fixedPoint",
                formItem: {
                    editorType: "dxNumberBox",
                    editorOptions: {
                        format: "fixedPoint",
                    }
                },
            },
            {
                dataField: "ash_uom_id",
                dataType: "text",
                caption: "Ash Unit",
                visible: false,
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: "/api/UOM/UOM/UomIdLookup",
                        onBeforeSend: function (method, ajaxOptions) {
                            ajaxOptions.xhrFields = { withCredentials: true };
                            ajaxOptions.beforeSend = function (request) {
                                request.setRequestHeader("Authorization", "Bearer " + token);
                            };
                        }
                    }),
                    valueExpr: "value",
                    displayExpr: "text"
                },
            },
            {
                dataField: "notes",
                label: {
                    text: "Remark"
                },
                formItem: {
                    colSpan: 2,
                },
                editorType: "dxTextArea",
                editorOptions: {
                    height: 50,
                },
                visible: false
            },
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
            allowAdding: true,
            allowUpdating: true,
            allowDeleting: true,
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
                url: "/api/General/Currency/UploadDocument",
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