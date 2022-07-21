$(function () {

    var token = $.cookie("Token");
    var areaName = "Equipment";
    var entityName = "EquipmentList";
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
        remoteOperations: false,
        allowColumnResizing: true,
        columnResizingMode: "widget",
        columns: [
            {
                dataField: "equipment_code",
                dataType: "string",
                caption: "Equipment Code",
                validationRules: [{
                    type: "required",
                    message: "This field is required."
                }],
            },
            {
                dataField: "equipment_name",
                dataType: "string",
                caption: "Equipment Name",
                formItem: {
                    colSpan: 2
                },
                sortIndex: 0,
                sortOrder: "asc"
            },
            {
                dataField: "equipment_type_id",
                dataType: "text",
                caption: "Equipment Type",
                validationRules: [{
                    type: "required",
                    message: "The Equipment Type field is required."
                }],
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: url + "/EquipmentTypeIdLookup",
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
                calculateSortValue: function (data) {
                    var value = this.calculateCellValue(data);
                    return this.lookup.calculateCellValue(value);
                },
            },
            {
                dataField: "vendor_id",
                dataType: "text",
                caption: "Owner",
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: url + "/VendorIdLookup",
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
                calculateSortValue: function (data) {
                    var value = this.calculateCellValue(data);
                    return this.lookup.calculateCellValue(value);
                },
            },
            {
                dataField: "capacity",
                dataType: "number",
                caption: "Capacity",
                validationRules: [{
                    type: "required",
                    message: "The Capacity field is required."
                }],
                width: 110
            },
            {
                dataField: "capacity_uom_id",
                dataType: "text",
                caption: "Capacity Unit",
                validationRules: [{
                    type: "required",
                    message: "The Capacity UoM field is required."
                }],
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: url + "/CapacityUomIdLookup",
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
                calculateSortValue: function (data) {
                    var value = this.calculateCellValue(data);
                    return this.lookup.calculateCellValue(value);
                },
                width: 110
            },
            {
                dataField: "vehicle_model",
                dataType: "string",
                caption: "Model"
            },
            {
                dataField: "vehicle_model_year",
                dataType: "number",
                caption: "Model Year"
            },
            {
                dataField: "vehicle_manufactured_year",
                dataType: "number",
                caption: "Manufactured Year"
            },
            {
                dataField: "status",
                dataType: "boolean",
                caption: "Is Active"
            }
        ],
        masterDetail: {
            enabled: true,
            template: function (container, options) {
                var currentRecord = options.data;
                var detailName = "EquipmentRate";
                var urlDetail = "/api/" + areaName + "/" + detailName;

                $("<div>")
                    .addClass("master-detail-caption")
                    .text("Cost Rates")
                    .appendTo(container);

                $("<div>")
                    .dxDataGrid({
                        dataSource: DevExpress.data.AspNet.createStore({
                            key: "id",
                            loadUrl: urlDetail + "/ByEquipmentId/" + encodeURIComponent(currentRecord.id),
                            insertUrl: urlDetail + "/InsertData",
                            updateUrl: urlDetail + "/UpdateData",
                            deleteUrl: urlDetail + "/DeleteData",
                            onBeforeSend: function (method, ajaxOptions) {
                                ajaxOptions.xhrFields = { withCredentials: true };
                                ajaxOptions.beforeSend = function (request) {
                                    request.setRequestHeader("Authorization", "Bearer " + token);
                                };
                            }
                        }),
                        remoteOperations: true,
                        allowColumnResizing: true,
                        columns: [
                            //{
                            //    dataField: "equipment_id",
                            //    allowEditing: false,
                            //    visible: false,
                            //    calculateCellValue: function () {
                            //        return currentRecord.id;
                            //    }
                            //},
                            {
                                dataField: "code",
                                dataType: "text",
                                caption: "Code",
                            },
                            {
                                dataField: "name",
                                dataType: "text",
                                caption: "Name",
                            },
                            {
                                dataField: "accounting_period_name",
                                dataType: "text",
                                caption: "Period Name",
                                allowEditing: false
                            },
                            {
                                dataField: "accounting_period_id",
                                dataType: "text",
                                caption: "Accounting Period",
                                visible: false,
                                validationRules: [{
                                    type: "required",
                                    message: "The field is required."
                                }],
                                lookup: {
                                    dataSource: DevExpress.data.AspNet.createStore({
                                        key: "value",
                                        loadUrl: urlDetail + "/AccountingPeriodIdLookup",
                                        onBeforeSend: function (method, ajaxOptions) {
                                            ajaxOptions.xhrFields = { withCredentials: true };
                                            ajaxOptions.beforeSend = function (request) {
                                                request.setRequestHeader("Authorization", "Bearer " + token);
                                            };
                                        }
                                    }),
                                    valueExpr: "value",
                                    displayExpr: "text"
                                }
                            },
                            
                            {
                                dataField: "currency_id",
                                dataType: "text",
                                caption: "Currency",
                                validationRules: [{
                                    type: "required",
                                    message: "The field is required."
                                }],
                                lookup: {
                                    dataSource: DevExpress.data.AspNet.createStore({
                                        key: "value",
                                        loadUrl: urlDetail + "/CurrencyIdLookup",
                                        onBeforeSend: function (method, ajaxOptions) {
                                            ajaxOptions.xhrFields = { withCredentials: true };
                                            ajaxOptions.beforeSend = function (request) {
                                                request.setRequestHeader("Authorization", "Bearer " + token);
                                            };
                                        }
                                    }),
                                    valueExpr: "value",
                                    displayExpr: "text"
                                }
                            },
                            {
                                dataField: "hourly_rate",
                                dataType: "number",
                                caption: "Hourly Rate"
                            },
                            {
                                dataField: "trip_rate",
                                dataType: "number",
                                caption: "Trip Rate"
                            },
                            {
                                dataField: "monthly_rate",
                                dataType: "number",
                                caption: "Monthly Rate"
                            },
                            {
                                dataField: "fuel_per_hour",
                                dataType: "number",
                                caption: "Fuel per Hour"
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
                        onInitNewRow: function (e) {
                            e.data.equipment_id = currentRecord.id;
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
                                    saveAs(new Blob([buffer], { type: 'application/octet-stream' }), detailName + '.xlsx');
                                });
                            });
                            e.cancel = true;
                        }
                    }).appendTo(container);
            }
        },
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
        onEditorPreparing: function (e) {
            if (e.parentType === "dataRow" && e.dataField == "equipment_code") {
                e.editorOptions.disabled = e.row.data.equipment_code != null
            }
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
                url: "/api/Equipment/EquipmentList/UploadDocument",
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