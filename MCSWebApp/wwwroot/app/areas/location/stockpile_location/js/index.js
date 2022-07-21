$(function () {

    var token = $.cookie("Token");
    var areaName = "Location";
    var entityName = "StockpileLocation";
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
                dataField: "business_area_id",
                dataType: "text",
                caption: "Business Area",
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
                        loadUrl: "/api/Location/BusinessArea/BusinessAreaIdLookup",
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
                sortOrder: "asc"
            },
            {
                dataField: "stockpile_location_code",
                dataType: "string",
                caption: "Stockpile Location Code",
                validationRules: [{
                    type: "required",
                    message: "This field is required."
                }]
            },
            {
                dataField: "stock_location_name",
                dataType: "string",
                caption: "Stockpile Location Name",
                validationRules: [{
                    type: "required",
                    message: "This field is required."
                }]
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
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: url + "/ProductIdLookup",
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
                dataField: "uom_id",
                dataType: "text",
                caption: "Unit",
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: url + "/UomIdLookup",
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
                dataField: "quality_sampling_id",
                dataType: "text",
                caption: "Quality Sampling",
                visible: false,
                lookup: {
                    dataSource: function (options) {
                        return {
                            store: DevExpress.data.AspNet.createStore({
                                key: "value",
                                loadUrl: url + "/QualitySamplingIdLookup",
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
                }
            },
            {
                dataField: "current_stock",
                dataType: "number",
                caption: "Current Stock",
                allowEditing: false
            },
            {
                dataField: "opening_date",
                dataType: "date",
                caption: "Opening Date",
                validationRules: [{
                    type: "required",
                    message: "The Opening Date field is required."
                }],
                hidingPriority: 1
            },
            {
                dataField: "closing_date",
                dataType: "date",
                caption: "Closing Date",
                hidingPriority: 0
            }
        ],
        masterDetail: {
            enabled: true,
            template: masterDetailTemplate
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
            if (e.parentType === "dataRow" && e.dataField == "stockpile_location_code") {
                e.editorOptions.disabled = e.row.data.stockpile_location_code != null
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

    function masterDetailTemplate(_, masterDetailOptions) {
        return $("<div>").dxTabPanel({
            items: [
                {
                    title: "Quality Sampling",
                    template: createQualitySamplingTab(masterDetailOptions.data)
                }
            ]
        });
    }

    function createQualitySamplingTab(masterDetailData) {
        return function () {
            let currentRecord = masterDetailData;
            let detailName = "StockpileLocation";
            let urlDetail = "/api/" + areaName + "/" + detailName;
            bargingTransactionData = currentRecord

            documentDataGrid = $("<div>")
                .dxDataGrid({
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "id",
                        loadUrl: urlDetail + "/ByStockpileLocationId/" + encodeURIComponent(currentRecord.id),
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
                            dataField: "quality_sampling_id",
                            caption: "Quality Sampling Id",
                            allowEditing: false,
                            visible: false,
                            calculateCellValue: function () {
                                return currentRecord.id;
                            }
                        },
                        {
                            dataField: "analyte_name",
                            dataType: "text",
                            caption: "Analyte",
                            validationRules: [{
                                type: "required",
                                message: "The field is required."
                            }]
                        },
                        {
                            dataField: "uom_id",
                            dataType: "text",
                            caption: "Unit",
                            validationRules: [{
                                type: "required",
                                message: "The field is required."
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
                            dataField: "analyte_value",
                            dataType: "number",
                            caption: "Value",
                            validationRules: [{
                                type: "required",
                                message: "The field is required."
                            }]
                        }
                    ],
                    onToolbarPreparing: function (e) {
                        let toolbarItems = e.toolbarOptions.items;

                        // Modifies an existing item
                        toolbarItems.forEach(function (item) {
                            if (item.name === "addRowButton") {
                                item.options = {
                                    icon: "plus",
                                    onClick: function (e) {
                                        openDocumentPopup()
                                    }
                                }
                            }

                            if (item.name === "editRowButton") {
                                item.options = {
                                    icon: "edit",
                                    onClick: function (e) {
                                        openDocumentPopup()
                                    }
                                }
                            }
                        });
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
                        e.data.shipping_transaction_id = currentRecord.id;
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
                });

            return documentDataGrid
        }
    }

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
                url: "/api/Location/StockpileLocation/UploadDocument",
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