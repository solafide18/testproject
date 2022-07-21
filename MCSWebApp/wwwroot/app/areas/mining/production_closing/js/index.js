$(function () {
    var token = $.cookie("Token");
    var areaName = "Mining";
    var entityName = "ProductionClosing";
    var url = "/api/" + areaName + "/" + entityName;    

    var from_date = null;
    var to_date = null;
    var source_location_id = null;
    var destination_location_id = null;

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
            },
            onInserting: function (values) {
                console.log(values);
            },
            onUpdating: function (values) {
                console.log(values);
            }
        }),
        remoteOperations: false,
        allowColumnResizing: true,
        columnResizingMode: "widget",
        dateSerializationFormat: "yyyy-MM-ddTHH:mm:ss",
        columns: [
            {
                dataField: "transaction_number",
                dataType: "string",
                caption: "Transaction Number",
                validationRules: [{
                    type: "required",
                    message: "Transaction Number is required."
                }],
                formItem: {
                    colSpan: 2
                },
                sortOrder: "asc"
            },
            {
                dataField: "transaction_date",
                dataType: "date",
                caption: "Transaction Date",
                validationRules: [{
                    type: "required",
                    message: "This field is required."
                }]
            },
            {
                dataField: "accounting_period_id",
                dataType: "text",
                caption: "Accounting Period",
             
                validationRules: [{
                    type: "required",
                    message: "This field is required."
                }],
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: "/api/Accounting/AccountingPeriod/AccountingPeriodIdLookup",
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
                dataField: "advance_contract_id",
                dataType: "text",
                caption: "Advance Contract",
                formItem: {
                    colSpan: 2
                },
                validationRules: [{
                    type: "required",
                    message: "This field is required."
                }],
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: "/api/ContractManagement/AdvanceContract/AdvanceContractIdLookup",
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
                setCellValue: function (rowData, value) {
                    rowData.advance_contract_id = value;
                },
                calculateSortValue: function (data) {
                    var value = this.calculateCellValue(data);
                    return this.lookup.calculateCellValue(value);
                }
            },
            {
                dataField: "advance_contract_reference_id",
                dataType: "text",
                caption: "Advance Contract Reference",
                formItem: {
                    colSpan: 2
                },
                validationRules: [{
                    type: "required",
                    message: "Advance Contract Reference is required."
                }],
                lookup: {
                    dataSource: function (options) {
                        var advId = "";

                        if (options !== undefined && options !== null) {
                            if (options.data !== undefined && options.data !== null) {
                                if (options.data.advance_contract_id !== undefined
                                    && options.data.advance_contract_id !== null) {
                                    advId = options.data.advance_contract_id
                                }
                            }
                        }
                        return {
                            store: DevExpress.data.AspNet.createStore({
                                key: "value",
                                loadUrl: "/api/ContractManagement/AdvanceContractReference/AdvanceContractReferenceIdLookupByAdvanceContractId?advance_contract_id=" + advId,
                                onBeforeSend: function (method, ajaxOptions) {
                                    ajaxOptions.xhrFields = { withCredentials: true };
                                    ajaxOptions.beforeSend = function (request) {
                                        request.setRequestHeader("Authorization", "Bearer " + token);
                                    };
                                }
                            }),
                        }
                    },
                    valueExpr: "value",
                    displayExpr: "text"
                },
                visible: false
            },
           
            {
                dataField: "from_date",
                dataType: "datetime",
                caption: "From Date",
                format: "yyyy-MM-dd HH:mm:ss"
            },
            {
                dataField: "to_date",
                dataType: "datetime",
                caption: "To Date",
                format: "yyyy-MM-dd HH:mm:ss"
            },
            {
                dataField: "volume",
                dataType: "number",
                caption: "Volume"
            },
            {
                dataField: "distance",
                dataType: "number",
                caption: "Distance (meter)"
            },
            {
                dataField: "note",
                dataType: "string",
                caption: "Note",
                formItem: {
                    colSpan: 2
                },
            }
        ],

        masterDetail: {
            enabled: true,
            template: function (container, options) {
                var currentRecord = options.data;
                var detailName = "ProductionClosing";
                var urlDetail = "/api/Mining/Production";

                $("<div>")
                    .addClass("master-detail-caption")
                    .text("Detail")
                    .appendTo(container);

                $("<div>")
                    .dxDataGrid({
                        dataSource: DevExpress.data.AspNet.createStore({
                            key: "id",
                            loadUrl: url + "/ProductionByTN?FromDate=" + currentRecord.from_date + "&ToDate=" + currentRecord.to_date,
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
                            {
                                dataField: "transaction_number",
                                dataType: "string",
                                caption: "Transaction Number",
                                allowEditing: false,
                                sortOrder: "asc"
                            },
                            {
                                dataField: "unloading_datetime",
                                dataType: "datetime",
                                caption: "DateTime",
                                validationRules: [{
                                    type: "required",
                                    message: "This field is required."
                                }],
                                setCellValue: function (rowData, value) {
                                    rowData.unloading_datetime = value;
                                }
                            },
                            {
                                dataField: "product_id",
                                dataType: "text",
                                caption: "Product",
                                validationRules: [{
                                    type: "required",
                                    message: "This field is required."
                                }],
                                lookup: {
                                    dataSource: function (options) {
                                        var _url = urlDetail + "/ProductIdLookup";

                                        if (options !== undefined && options !== null) {
                                            if (options.data !== undefined && options.data !== null) {
                                                if (options.data.process_flow_id !== undefined
                                                    && options.data.process_flow_id !== null) {
                                                    _url += "?ProcessFlowId=" + encodeURIComponent(options.data.process_flow_id);
                                                }
                                            }
                                        }

                                        return {
                                            store: DevExpress.data.AspNet.createStore({
                                                key: "value",
                                                loadUrl: _url,
                                                onBeforeSend: function (method, ajaxOptions) {
                                                    ajaxOptions.xhrFields = { withCredentials: true };
                                                    ajaxOptions.beforeSend = function (request) {
                                                        request.setRequestHeader("Authorization", "Bearer " + token);
                                                    };
                                                }
                                            })
                                        }
                                    },
                                    valueExpr: "value",
                                    displayExpr: "text"
                                },
                                setCellValue: function (rowData, value) {
                                    rowData.product_id = value;
                                },
                                calculateSortValue: function (data) {
                                    var value = this.calculateCellValue(data);
                                    return this.lookup.calculateCellValue(value);
                                }
                            },
                            {
                                dataField: "unloading_quantity",
                                dataType: "number",
                                caption: "Quantity",
                                format: {
                                    type: "fixedPoint",
                                    precision: 3
                                },
                                formItem: {
                                    editorType: "dxNumberBox",
                                    editorOptions: {
                                        format: {
                                            type: "fixedPoint",
                                            precision: 3
                                        }
                                    }
                                },
                                validationRules: [{
                                    type: "required",
                                    message: "This field is required."
                                }]
                            },
                            {
                                dataField: "uom_id",
                                dataType: "text",
                                caption: "Unit",
                                validationRules: [{
                                    type: "required",
                                    message: "This field is required."
                                }],
                                lookup: {
                                    dataSource: DevExpress.data.AspNet.createStore({
                                        key: "value",
                                        loadUrl: urlDetail + "/UomIdLookup",
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
                                dataField: "distance",
                                dataType: "number",
                                caption: "Distance (meter)"
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
                        onInitNewRow: function (e) {
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

        onEditorPreparing: function (e) {
            if (e.parentType === "dataRow" && e.dataField === "from_date") {

                let standardHandler = e.editorOptions.onValueChanged;
                let index = e.row.rowIndex;
                let grid = e.component;

                e.editorOptions.onValueChanged = function (e) { // Overiding the standard handler
                    //sessionStorage.setItem("from_date", e.value);
                    //sessionStorage.setItem("to_date", e.value);

                    //from_date = sessionStorage.getItem("from_date");
                    //to_date = sessionStorage.getItem("to_date");

                    if (e.value !== null && e.value !== undefined) {
                        from_date = new Date(e.value);
                    };

                    if (from_date !== null && to_date !== null) {
                        let cari = "/api/Mining/ProductionClosing/ProductionTotal"
                            + "?FromDate=" + encodeURIComponent(from_date.toISOString())
                            + "&ToDate=" + encodeURIComponent(to_date.toISOString());
                        $.ajax({
                            url: cari,
                            type: 'GET',
                            contentType: "application/json",
                            beforeSend: function (xhr) {
                                xhr.setRequestHeader("Authorization", "Bearer " + token);
                            },
                            success: function (response) {
                                //console.log(response.data);
                                let record = response.data[0];
                                // Set its corresponded field's value
                                grid.cellValue(index, "from_date", from_date);
                                grid.cellValue(index, "to_date", to_date)
                                grid.cellValue(index, "volume", record.unloading_quantity);
                                grid.cellValue(index, "distance", record.distance)
                            }
                        });
                    };

                    standardHandler(e) // Calling the standard handler to save the edited value
                }
            };

            if (e.parentType === "dataRow" && e.dataField === "to_date") {

                let standardHandler = e.editorOptions.onValueChanged;
                let index = e.row.rowIndex;
                let grid = e.component;

                e.editorOptions.onValueChanged = function (e) { // Overiding the standard handler
                    //sessionStorage.setItem("from_date", e.value);
                    //sessionStorage.setItem("to_date", e.value);

                    //from_date = sessionStorage.getItem("from_date");
                    //to_date = sessionStorage.getItem("to_date");

                    if (e.value !== null && e.value !== undefined) {
                        to_date = new Date(e.value);
                    };

                    if (from_date !== null && to_date !== null) {
                        let cari = "/api/Mining/ProductionClosing/ProductionTotal"
                            + "?FromDate=" + encodeURIComponent(from_date.toISOString())
                            + "&ToDate=" + encodeURIComponent(to_date.toISOString());
                        $.ajax({
                            url: cari,
                            type: 'GET',
                            contentType: "application/json",
                            beforeSend: function (xhr) {
                                xhr.setRequestHeader("Authorization", "Bearer " + token);
                            },
                            success: function (response) {
                                //console.log(response.data);
                                let record = response.data[0];
                                // Set its corresponded field's value
                                grid.cellValue(index, "from_date", from_date);
                                grid.cellValue(index, "to_date", to_date)
                                grid.cellValue(index, "volume", record.unloading_quantity);
                                grid.cellValue(index, "distance", record.distance)
                            }
                        });
                    };

                    standardHandler(e) // Calling the standard handler to save the edited value
                }
            };
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
            mode: "popup",
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
                url: "/api/Mining/ProductionClosing/UploadDocument",
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