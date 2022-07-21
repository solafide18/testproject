﻿$(function () {

    var token = $.cookie("Token");
    var areaName = "ContractManagement";
    var entityName = "AdvanceContractValuation";
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
                dataField: "advance_contract_valuation_number",
                dataType: "string",
                caption: "Valuation Number",
                validationRules: [{
                    type: "required",
                    message: "This field is required."
                }]
            },
            {
                dataField: "advance_contract_id",
                dataType: "text",
                caption: "Advance Contract",
                lookup: {
                    dataSource: function (options) {
                        return {
                            store: DevExpress.data.AspNet.createStore({
                                key: "value",
                                loadUrl: "/api/" + areaName + "/AdvanceContract/AdvanceContractIdLookup",
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
                onValueChangeAction: function (e) {
                    var newItem = e.selectedItem;
                },
                setCellValue: function (rowData, value) {
                    rowData.advance_contract_id = value;
                    rowData.advance_contract_reference_id = null;
                },
            },
            {
                dataField: "progress_claim_name",
                dataType: "string",
                caption: "Advance Contract Reference",
            },
            {
                dataField: "advance_contract_reference_id",
                dataType: "text",
                caption: "Advance Contract Reference",
                validationRules: [{
                    type: "required",
                    message: "Advance Contract Reference is required."
                }],
                //editorOptions: { disabled: true },
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
                                loadUrl: "/api/" + areaName + "/AdvanceContractReference/AdvanceContractReferenceIdLookupByAdvanceContractId?advance_contract_id=" + advId,
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
                dataField: "valuation_date",
                label: {
                    text: "Valuation Date"
                },
                editorType: "dxDateBox",
                editorOptions: {
                    format: "fixedPoint",
                },
            },
            {
                dataField: "employee_id",
                dataType: "text",
                caption: "Employee",
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: "/api/General/Employee/EmployeeIdLookup",
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
                }
            },
            {
                dataField: "notes",
                dataType: "string",
                caption: "Notes",
            },
            {
                type: "buttons",
                width: 110,
                buttons: [
                    "edit",
                    "delete",
                    {
                        hint: "Download",
                        icon: "fas fa-download",
                        visible: true,
                        onClick: function (e) {
                            // Download file from Ajax. Ref: https://stackoverflow.com/a/9970672
                            let documentData = e.row.data
                            //let documentName = /[^\\]*$/.exec(documentData.file_name)[0]
                            //console.log(documentData);
                            //return;
                            let xhr = new XMLHttpRequest()
                            xhr.open("GET", "/api/ContractManagement/AdvanceContractValuation/DownloadDocument/" + documentData.id, true)
                            xhr.responseType = "blob"
                            xhr.setRequestHeader("Authorization", "Bearer " + token)

                            xhr.onload = function (e) {
                                let blobURL = window.webkitURL.createObjectURL(xhr.response)

                                let a = document.createElement("a")
                                a.href = blobURL
                                //a.download = documentName
                                a.download = "AdvanceContractValuation.xlsx"
                                document.body.appendChild(a)
                                a.click()
                            };

                            xhr.send()
                        }
                    }
                ]
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
            mode: "popup",
            allowAdding: true,
            allowUpdating: true,
            allowDeleting: true,
            useIcons: true,
            form: {
                //colCount: 3,
                items: [
                    {
                        dataField: "advance_contract_valuation_number",
                        colSpan: 2
                    },
                    {
                        dataField: "advance_contract_id",
                        colSpan: 2
                    },
                    {
                        dataField: "advance_contract_reference_id",
                        colSpan: 2
                    },
                    {
                        dataField: "valuation_date",
                        label: {
                            text: "Valuation Date"
                        },
                        editorType: "dxDateBox",
                        editorOptions: {
                            format: "fixedPoint",
                        },
                    },
                    {
                        dataField: "employee_id",
                        colSpan: 2
                    },
                    {
                        dataField: "notes",
                        colSpan: 2
                    },
                ]
            }
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
            if (e.parentType === "dataRow") {
                
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
            items: [{
                title: "Advance Contract Valuation Detail",
                template: createValuationDetailTabTemplate(masterDetailOptions.data)
            }]
        });
    };

    function createValuationDetailTabTemplate(masterDetailData) {
        return function () {
            let currentRecord = masterDetailData;
            console.log("currentRecord");
            console.log(currentRecord);
            console.log(currentRecord.advance_contract_reference_id);
            let detailName = "AdvanceContractValuationDetail";
            let urlDetail = "/api/" + areaName + "/" + detailName;

            return $("<div>")
                .dxDataGrid({
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "id",
                        //loadUrl: urlDetail + "/DataGrid/" + encodeURIComponent(currentRecord.id),
                        loadUrl: urlDetail + "/DataGridByAdvanceValuationId?advanceContractValuationId=" + encodeURIComponent(currentRecord.id),
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
                        //    dataField: "advance_contract_reference_id",
                        //    caption: "Advance Contract Reference Id",
                        //    allowEditing: false,
                        //    visible: false,
                        //    calculateCellValue: function () {
                        //        return currentRecord.id;
                        //    }
                        //},
                        {
                            dataField: "advance_contract_reference_detail_id",
                            dataType: "text",
                            caption: "Charge Name",
                            validationRules: [{
                                type: "required",
                                message: "This field is required."
                            }],
                            lookup: {
                                dataSource: DevExpress.data.AspNet.createStore({
                                    key: "value",
                                    loadUrl: "/api/" + areaName + "/AdvanceContractReferenceDetail/AdvanceContractReferenceDetailIdLookupByAdvanceContractReferenceId?advance_contract_reference_id=" + currentRecord.advance_contract_reference_id,
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
                                rowData.advance_contract_reference_detail_id = value;
                            },
                        },
                        {
                            dataField: "value",
                            dataType: "number",
                            caption: "Value",
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
                                message: "The field is required."
                            }]
                        },
                        {
                            dataField: "convertion_amount",
                            dataType: "number",
                            caption: "Final Value",
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
                                message: "The field is required."
                            }]
                        },
                        {
                            dataField: "formula",
                            dataType: "text",
                            caption: "Formula",
                            allowEditing: false,
                            visible: false,
                            formItem: {
                                visible: false
                            }
                        },
                        {
                            dataField: "formula_trace",
                            dataType: "text",
                            caption: "Formula Trace",
                            allowEditing: false,
                            visible: false
                        },
                        {
                            type: "buttons",
                            buttons: ["edit", "delete"]
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
                        e.data.advance_contract_valuation_id = currentRecord.id;
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
                    },
                    onEditorPreparing: function (e) {
                        if (e.parentType === "dataRow") {

                        }
                        if (e.parentType === "dataRow" && e.dataField == "advance_contract_reference_detail_id") {
                            const defaultValueChangeHandler = e.editorOptions.onValueChanged;
                            let index = e.row.rowIndex;
                            let grid = e.component;
                            e.editorName = "dxSelectBox"; // Change the editor's type
                            e.editorOptions.onValueChanged = function (args) {  // Override the default handler
                                console.log("args.value");
                                console.log(args.value);

                                grid.cellValue(index, "advance_contract_reference_detail_id", args.value);

                                $.ajax({
                                    //url: "/api/ContractManagement/AdvanceContractDetail/GetByAdvanceContractId?AdvanceContractId=" + encodeURIComponent($advanceContractId),
                                    url: "/api/" + areaName + "/AdvanceContractReferenceDetail/CalculateFormulaById?recordId=" + args.value,
                                    type: 'GET',
                                    contentType: "application/json",
                                    beforeSend: function (xhr) {
                                        xhr.setRequestHeader("Authorization", "Bearer " + token);
                                    },
                                    success: function (r) {
                                        console.log("CalculateFormulaById: ", r);
                                        if (r.success) {
                                            grid.cellValue(index, "advance_contract_charge_id", args.value);
                                            grid.cellValue(index, "value", (r.value));
                                            grid.cellValue(index, "convertion_amount", (r.convertion_amount));
                                            grid.cellValue(index, "formula_trace", (r.formula));
                                        } else {
                                            toastr["error"](r.message ?? "Error");
                                        }
                                    }
                                });
                            }
                        }
                    },
                });
        }
    };

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
                url: url + "/UploadDocument",
                type: 'POST',
                cache: false,
                contentType: "application/json",
                data: JSON.stringify(formData),
                headers: {
                    "Authorization": "Bearer " + token
                }
            }).done(function (result) {
                alert('File berhasil di-upload!');
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