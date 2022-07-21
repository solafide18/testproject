$(function () {

    var token = $.cookie("Token");
    var areaName = "Port";
    var entityName = "Shipping";
    var url = "/api/" + areaName + "/" + entityName;
    const maxFileSize = 52428800;

    var shippingTransactionData;

    function formatTanggal(x) {
        theDate = new Date(x);
        formatted_date = theDate.getFullYear() + "-" + (theDate.getMonth() + 1).toString().padStart(2, "0") + "-" 
            + theDate.getDate().toString().padStart(2, "0") + " " + theDate.getHours().toString().padStart(2, "0")
            + ":" + theDate.getMinutes().toString().padStart(2, "0");
        return formatted_date;
    }

    var tgl1 = sessionStorage.getItem("ShipLoadingDate1");
    var tgl2 = sessionStorage.getItem("ShipLoadingDate2");

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
            sessionStorage.setItem("ShipLoadingDate1", formatTanggal(firstDay));
            _loadUrl = url + "/Loading/DataGrid/" + encodeURIComponent(formatTanggal(firstDay))
                + "/" + encodeURIComponent(formatTanggal(lastDay));
        }
    });

    $("#date-box2").dxDateBox({
        type: "datetime",
        displayFormat: 'dd MMM yyyy HH:mm',
        value: lastDay,
        onValueChanged: function (data) {
            lastDay = new Date(data.value);
            sessionStorage.setItem("ShipLoadingDate2", formatTanggal(lastDay));
            _loadUrl = url + "/Loading/DataGrid/" + encodeURIComponent(formatTanggal(firstDay))
                + "/" + encodeURIComponent(formatTanggal(lastDay));
        }
    });

    $('#btnView').on('click', function () {
        location.reload();
    })

    var _loadUrl = url + "/Loading/DataGrid/" + encodeURIComponent(formatTanggal(firstDay))
        + "/" + encodeURIComponent(formatTanggal(lastDay));

    $("#grid").dxDataGrid({
        dataSource: DevExpress.data.AspNet.createStore({
            key: "id",
            //loadUrl: url + "/Loading/DataGrid",
            loadUrl: _loadUrl,
            insertUrl: url + "/Loading/InsertData",
            updateUrl: url + "/Loading/UpdateData",
            deleteUrl: url + "/Loading/DeleteData",
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
                dataField: "transaction_number",
                dataType: "string",
                caption: "Transaction Number",
                allowEditing: false,
                sortOrder: "asc"
            },
            {
                dataField: "accounting_period_id",
                dataType: "text",
                caption: "Accounting Period",
                visible: false,
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: url + "/AccountingPeriodIdLookup",
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
                dataField: "despatch_order_id",
                dataType: "text",
                caption: "Despatch Order",
                validationRules: [{
                    type: "required",
                    message: "This field is required."
                }],
                lookup: {
                    dataSource: function (options) {
                        return {
                            store: DevExpress.data.AspNet.createStore({
                                key: "value",
                                loadUrl: url + "/DespatchOrderIdLookup",
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
                    rowData.despatch_order_id = value;
                },
                calculateSortValue: function (data) {
                    var value = this.calculateCellValue(data);
                    return this.lookup.calculateCellValue(value);
                },
            },
            {
                dataField: "process_flow_id",
                dataType: "text",
                caption: "Process Flow",
                //formItem: {
                //    colSpan: 2
                //},
                validationRules: [{
                    type: "required",
                    message: "This field is required."
                }],
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: url + "/ProcessFlowIdLookup",
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
                    rowData.process_flow_id = value;
                },
                calculateSortValue: function (data) {
                    var value = this.calculateCellValue(data);
                    return this.lookup.calculateCellValue(value);
                },
                visible: false
            },
            {
                dataField: "ship_location_id",
                dataType: "text",
                caption: "Vessel",
                //formItem: {
                //    colSpan: 2
                //},
                allowEditing: false,
                lookup: {
                    dataSource: function (options) {
                        var _url = url + "/ShipLocationIdLookup";

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
                calculateSortValue: function (data) {
                    var value = this.calculateCellValue(data);
                    return this.lookup.calculateCellValue(value);
                },
            },
            {
                dataField: "quantity",
                dataType: "number",
                caption: "Original Source Quantity",
                format: "fixedPoint",
                formItem: {
                    editorType: "dxNumberBox",
                    editorOptions: {
                        format: "fixedPoint",
                    }
                },
                allowEditing: false
            },
            {
                dataField: "uom_id",
                dataType: "text",
                caption: "Unit",
                width: "100px",
                validationRules: [{
                    type: "required",
                    message: "This field is required."
                }],
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
                },
                calculateSortValue: function (data) {
                    var value = this.calculateCellValue(data);
                    return this.lookup.calculateCellValue(value);
                },
            },

            {
                dataField: "product_name",
                dataType: "string",
                caption: "Product Name",
                visible: false,
                allowEditing: false
            },
            //{
            //    dataField: "product_id",
            //    dataType: "text",
            //    caption: "Product",
            //    validationRules: [{
            //        type: "required",
            //        message: "This field is required."
            //    }],
            //    lookup: {
            //        dataSource: function (options) {
            //            var _url = url + "/ProductIdLookup";

            //            if (options !== undefined && options !== null) {
            //                if (options.data !== undefined && options.data !== null) {
            //                    if (options.data.process_flow_id !== undefined
            //                        && options.data.process_flow_id !== null) {
            //                        _url += "?ProcessFlowId=" + encodeURIComponent(options.data.process_flow_id);
            //                    }
            //                }
            //            }

            //            return {
            //                store: DevExpress.data.AspNet.createStore({
            //                    key: "value",
            //                    loadUrl: _url,
            //                    onBeforeSend: function (method, ajaxOptions) {
            //                        ajaxOptions.xhrFields = { withCredentials: true };
            //                        ajaxOptions.beforeSend = function (request) {
            //                            request.setRequestHeader("Authorization", "Bearer " + token);
            //                        };
            //                    }
            //                })
            //            }
            //        },
            //        valueExpr: "value",
            //        displayExpr: "text"
            //    },
            //    calculateSortValue: function (data) {
            //        var value = this.calculateCellValue(data);
            //        return this.lookup.calculateCellValue(value);
            //    },
            //},
            {
                dataField: "equipment_id",
                dataType: "text",
                caption: "Equipment",
                visible: false,
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: url + "/EquipmentIdLookup",
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
                dataField: "quality_sampling_id",
                dataType: "text",
                caption: "Quality Sampling",
                formItem: {
                    colSpan: 2
                },
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
                },
                calculateSortValue: function (data) {
                    var value = this.calculateCellValue(data);
                    return this.lookup.calculateCellValue(value);
                },
            },
            {
                dataField: "delivery_term_name",
                caption: "Delivery Term",
                dataType: "string",
                visible: false,
                allowEditing: false,
            },
            //{
            //    dataField: "distance",
            //    dataType: "number",
            //    caption: "Distance",
            //    format: "fixedPoint",
            //    formItem: {
            //        editorType: "dxNumberBox",
            //        editorOptions: {
            //            format: "fixedPoint",
            //        }
            //    }
            //},
            {
                dataField: "note",
                dataType: "string",
                caption: "Note",
                visible: false,
                formItem: {
                    colSpan: 2,
                    editorType: "dxTextArea"
                }
            },
            {
                dataField: "ref_work_order",
                dataType: "string",
                caption: "Ref.Work Order",
                visible: false,
            },
            {
                dataField: "arrival_datetime",
                dataType: "datetime",
                caption: "Arrival DateTime",
                format: "yyyy-MM-dd HH:mm:ss",
                visible: false
            },
            {
                dataField: "berth_datetime",
                dataType: "datetime",
                caption: "Alongside DateTime",
                format: "yyyy-MM-dd HH:mm:ss",
                visible: false
            },
            {
                dataField: "start_datetime",
                dataType: "datetime",
                caption: "Commenced Loading DateTime",
                format: "yyyy-MM-dd HH:mm:ss",
                sortOrder: "desc",
                validationRules: [{
                    type: "required",
                    message: "This field is required."
                }]
            },
            {
                dataField: "end_datetime",
                dataType: "datetime",
                caption: "Completed Loading DateTime",
                format: "yyyy-MM-dd HH:mm:ss",
                //validationRules: [{
                //    type: "required",
                //    message: "This field is required."
                //}]
            },
            {
                dataField: "unberth_datetime",
                dataType: "datetime",
                caption: "Cast Off DateTime",
                format: "yyyy-MM-dd HH:mm:ss",
                visible: false,
            },
            {
                dataField: "departure_datetime",
                dataType: "datetime",
                caption: "Departure DateTime",
                format: "yyyy-MM-dd HH:mm:ss",
                visible: false
            },
            {
                dataField: "draft_survey_id",
                dataType: "text",
                caption: "Draft Survey",
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: "/api/SurveyManagement/COW/DraftSurveyIdLookup",
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
                visible: false
            },
            {
                dataField: "draft_survey_number",
                dataType: "string",
                caption: "Draft Survey Number",
                //visible: false
            },
            {
                dataField: "original_quantity",
                dataType: "number",
                caption: "Draft Survey Quantity",
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
                //validationRules: [{
                //    type: "required",
                //    message: "The field is required."
                //}],
                visible: false
            },
            {
                dataField: "despatch_order_link",
                dataType: "string",
                caption: "Despatch Order",
                visible: false,
                allowFiltering: false
            },
            //initial draft survey
            {
                dataField: "initial_draft_survey",
                dataType: "datetime",
                caption: "Initial Draft Survey",
                format: "yyyy-MM-dd HH:mm:ss",
                visible: false
            },
            //Final draft survey
            {
                dataField: "final_draft_survey",
                dataType: "datetime",
                caption: "Final Draft Survey",
                format: "yyyy-MM-dd HH:mm:ss",
                visible: false
            },
            {
                dataField: "sales_contract_id",
                dataType: "string",
                caption: "Contract Number",
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: "/api/Sales/SalesContract/SalesContractIdLookup",
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
                    rowData.sales_contract_id = value
                },
                calculateSortValue: function (data) {
                    var value = this.calculateCellValue(data);
                    return this.lookup.calculateCellValue(value);
                },
                visible: true,
                allowEditing: false
            },
            {
                dataField: "customer_id",
                dataType: "string",
                caption: "Buyer",
                //visible: false,
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: "/api/Sales/Customer/CustomerIdLookup",
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
                allowEditing: false
            },
            {
                dataField: "source_location_id",
                dataType: "text",
                caption: "Source Location",
                visible: false,
                //validationRules: [{
                //    type: "required",
                //    message: "This field is required."
                //}],
                lookup: {
                    dataSource: function (options) {
                        var _url = "/api/Transport/Barge/BargeIdLookup";
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
                    rowData.source_location_id = value;
                },
                calculateSortValue: function (data) {
                    var value = this.calculateCellValue(data);
                    return this.lookup.calculateCellValue(value);
                },
            },
            {
                dataField: "imo_number",
                caption: "IMO Number",
                dataType: "string",
                visible: false,
                allowEditing: false
            },
            {
                dataField: "is_geared",
                dataType: "boolean",
                caption: "Is Geared",
                visible: true,
                allowEditing: false
            },
            {
                dataField: "owner_name",
                caption: "Owner",
                dataType: "string",
                visible: false,
                allowEditing: false
            },
            {
                dataField: "si_number",
                caption: "SI Number",
                dataType: "string",
                visible: false,
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: url + "/ShippingInstructionIdLookup",
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
                dataField: "si_date",
                dataType: "date",
                caption: "SI Date",
                allowEditing: false,
                visible: false
            },
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
                itemType: "group",
                caption: "Loading",
                items: [
                    {
                        dataField: "transaction_number"
                    },
                    {
                        dataField: "process_flow_id",
                    },
                    {
                        dataField: "ref_work_order",
                    },
                    //{
                        //dataField: "accounting_period_id"
                    //},
                    {
                        dataField: "despatch_order_id",
                    },
                    {
                        dataField: "delivery_term_name",
                    },
                    {
                        dataField: "despatch_order_link",
                        editorType: "dxButton",
                        editorOptions: {
                            text: "See Despatch Order Detail",
                            disabled: true
                        }
                    },
                    {
                        dataField: "sales_contract_id",
                    },
                    {
                        dataField: "customer_id",
                    },
                    {
                        dataField: "si_number",
                    },
                    {
                        dataField: "si_date",
                    },
                    
                    //{
                    //    dataField: "source_location_id",
                    //},
                   
                    {
                        dataField: "imo_number",
                    },
                    {
                        dataField: "is_geared",
                    },
                    {
                        dataField: "ship_location_id"
                    },
                    {
                        dataField: "owner_name",
                    },
                    {
                        dataField: "product_name",
                    },
                    {
                        dataField: "equipment_id",
                    },
                    {
                        dataField: "draft_survey_number",
                    },
                    {

                    },
                    {
                        dataField: "original_quantity",
                    },
           
                    {
                        dataField: "uom_id",
                    },
                    
                    //{
                    //    dataField: "quality_sampling_id",
                    //},
                    {
                        dataField: "initial_draft_survey",
                    },
                    {
                        dataField: "final_draft_survey",
                    },
                    //{
                    //    dataField: "distance",
                    //},
                    {
                        dataField: "note",
                    },
                    {
                        dataField: "arrival_datetime",
                    },
                    {
                        dataField: "berth_datetime",
                    },
                    {
                        dataField: "start_datetime",
                    },
                    {
                        dataField: "end_datetime",
                    },
                    {
                        dataField: "unberth_datetime",
                    },
                    {
                        dataField: "departure_datetime",
                    },
                    {
                        dataField: "quantity",
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
            // Enable and Set Despatch Order Link button after Despatch Order selected
            if (e.parentType === "dataRow" && e.dataField == "despatch_order_link") {
                if (e.row.data.despatch_order_id) {
                    let despatchOrderId = e.row.data.despatch_order_id

                    e.editorOptions.onClick = function (e) {
                        window.open("/Sales/DespatchOrder/Index?Id=" + despatchOrderId + "&openEditingForm=true", "_blank")
                    }
                    e.editorOptions.disabled = false
                }
            }

            if (e.parentType === "dataRow") {
                e.editorOptions.disabled = e.row.data && e.row.data.accounting_period_is_closed;
            }
            if (e.dataField === "despatch_order_id" && e.parentType == "dataRow") {
                let standardHandler = e.editorOptions.onValueChanged
                let index = e.row.rowIndex
                let grid = e.component
                let rowData = e.row.data

                e.editorOptions.onValueChanged = function (e) { // Overiding the standard handler

                    // Get its value (Id) on value changed
                    let despatchOrderId = e.value


                    // Get another data from API after getting the Id
                    $.ajax({
                        url: '/api/Sales/DespatchOrder/DataDetail?Id=' + despatchOrderId,
                        type: 'GET',
                        contentType: "application/json",
                        beforeSend: function (xhr) {
                            xhr.setRequestHeader("Authorization", "Bearer " + token);
                        },
                        success: function (response) {
                            /*let record = response.data[0]*/
                            let record = response.data;
                            //console.log(record);
                            // Set its corresponded field's value
                            grid.cellValue(index, "delivery_term_name", record.delivery_term_name)
                            grid.cellValue(index, "ship_location_id", record.vessel_name != null ? record.vessel_id : "")
                            grid.cellValue(index, "product_name", record.product_name)
                            grid.cellValue(index, "sales_contract_id", record.sales_contract_id)
                            grid.cellValue(index, "customer_id", record.customer_id)
                            grid.cellValue(index, "imo_number", record.imo_number)
                            grid.cellValue(index, "is_geared", record.is_geared)
                            grid.cellValue(index, "owner_name", record.owner_name)
                        }
                    })
                    standardHandler(e) // Calling the standard handler to save the edited value
                }
            }
            if (e.dataField === "si_number" && e.parentType == "dataRow") {
                let standardHandler = e.editorOptions.onValueChanged
                let index = e.row.rowIndex
                let grid = e.component
                let rowData = e.row.data

                e.editorOptions.onValueChanged = function (e) { // Overiding the standard handler

                    // Get its value (Id) on value changed
                    let siNumber = e.value

                    // Get another data from API after getting the Id
                    $.ajax({
                        url: '/api/Sales/ShippingInstruction/DataDetail?Id=' + siNumber,
                        type: 'GET',
                        contentType: "application/json",
                        beforeSend: function (xhr) {
                            xhr.setRequestHeader("Authorization", "Bearer " + token);
                        },
                        success: function (response) {
                            //alert('abc');
                            //console.log('abc');
                            let record = response.data;
                            //let record = response.data[0]
                            //console.log(record);
                            // Set its corresponded field's value


                            grid.cellValue(index, "si_date", record.shipping_instruction_date)

                            /*if (record.barge_name != null) {
                                grid.cellValue(index, "destination_location_id", record.barge_name);
                                
                            }*/
                        }
                    })
                    standardHandler(e) // Calling the standard handler to save the edited value
                }
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
                    title: "Sources",
                    template: createSourcesTabTemplate(masterDetailOptions.data)
                },
                {
                    title: "Documents",
                    template: createDocumentsTabTemplate(masterDetailOptions.data)
                },
                //{
                //    title: "Quality Sampling",
                //    template: createQualitySamplingTab(masterDetailOptions.data)
                //}
            ]
        });
    }

    function createSourcesTabTemplate(masterDetailData) {
        return function () {
            let currentRecord = masterDetailData;
            let detailName = "ShippingDetail";
            let urlDetail = "/api/" + areaName + "/" + detailName;

            return $("<div id='grid-shipping-transaction-detail'>")
                .dxDataGrid({
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "id",
                        loadUrl: urlDetail + "/ByShippingId/" + encodeURIComponent(currentRecord.id),
                        insertUrl: urlDetail + "/Loading/InsertData",
                        updateUrl: urlDetail + "/Loading/UpdateData",
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
                            dataField: "shipping_transaction_id",
                            caption: "Shipping Transaction Id",
                            allowEditing: false,
                            visible: false,
                            formItem: {
                                visible: false
                            },
                            calculateCellValue: function () {
                                return currentRecord.id;
                            }
                        },
                        {
                            dataField: "transaction_number",
                            dataType: "string",
                            caption: "Transaction Number",
                            allowEditing: false
                        },
                        {
                            dataField: "detail_location_id",
                            dataType: "text",
                            caption: "Source",
                            validationRules: [{
                                type: "required",
                                message: "This field is required."
                            }],
                            lookup: {
                                dataSource: function (options) {
                                    var _url = urlDetail + "/Loading/SourceLocationIdLookup";

                                    if (currentRecord.despatch_order_id !== undefined && currentRecord.despatch_order_id !== null) {
                                        _url += "?DespatchOrderId=" + encodeURIComponent(currentRecord.despatch_order_id);
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
                                rowData.detail_location_id = value;
                                rowData.survey_id = null;
                            }
                        },
                        {
                            dataField: "quantity",
                            dataType: "number",
                            caption: "Quantity",
                            format: "fixedPoint",
                            formItem: {
                                editorType: "dxNumberBox",
                                editorOptions: {
                                    format: "fixedPoint",
                                }
                            },
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
                            dataField: "reference_number",
                            dataType: "string",
                            caption: "Reference Number"
                        },
                        {
                            dataField: "start_datetime",
                            dataType: "datetime",
                            caption: "Commenced Unloading Date",
                            format: "yyyy-MM-dd HH:mm"
                        },
                        {
                            dataField: "end_datetime",
                            dataType: "datetime",
                            caption: "Complete Unloading Date",
                            format: "yyyy-MM-dd HH:mm"
                        },
                        {
                            dataField: "equipment_id",
                            dataType: "text",
                            caption: "Equipment",
                            lookup: {
                                dataSource: DevExpress.data.AspNet.createStore({
                                    key: "value",
                                    loadUrl: urlDetail + "/EquipmentIdLookup",
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
                            visible: false
                        },
                        {
                            dataField: "hour_usage",
                            dataType: "number",
                            caption: "Hour Usage"
                        },
                        {
                            dataField: "survey_id",
                            dataType: "text",
                            caption: "Survey",
                            lookup: {
                                dataSource: function (options) {
                                    var _url = urlDetail + "/SurveyIdLookup";

                                    if (options !== undefined && options !== null) {
                                        if (options.data !== undefined && options.data !== null) {
                                            if (options.data.detail_location_id !== undefined
                                                && options.data.detail_location_id !== null) {
                                                _url += "?LocationId=" + encodeURIComponent(options.data.detail_location_id);
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
                            }
                        },
                        //{
                        //    dataField: "final_quantity",
                        //    dataType: "number",
                        //    caption: "Final Quantity",
                        //    allowEditing: false
                        //},
                        {
                            dataField: "final_quantity",
                            dataType: "number",
                            caption: "Return Cargo Quantity"
                        },
                        {
                            dataField: "note",
                            dataType: "string",
                            caption: "Note",
                            formItem: {
                                colSpan: 2,
                                editorType: "dxTextArea"
                            }
                        },
                        {
                            dataField: "barging_transaction_number",
                            dataType: "string",
                            caption: "Barging Transaction Number",
                            allowEditing: false
                        },
                    ],
                    onContentReady: function (e) {
                        $("#btn-fetch").remove();
                        var $customButton = $('<div id="btn-fetch">').dxButton({
                            icon: 'refresh',
                            text: "Fetch",
                            onClick: function () {
                                console.log(masterDetailData);
                                //$("#grid-shipping-transaction-detail").dxDataGrid("getDataSource").reload();
                                $.ajax({
                                    url: '/api/Port/Barging/FetchBargingTransactionLoadingIntoShppingTransactionDetail/' + masterDetailData.despatch_order_id + '/' + masterDetailData.id,
                                    type: 'GET',
                                    contentType: "application/json",
                                    headers: {
                                        "Authorization": "Bearer " + token
                                    },
                                }).done(function (result) {
                                    if (result.status.success) {
                                        Swal.fire("Success!", "Fetching Data successfully.", "success");
                                        $("#grid-shipping-transaction-detail").dxDataGrid("getDataSource").reload();
                                    } else {
                                        Swal.fire("Error !", result.message, "error");
                                    }
                                }).fail(function (jqXHR, textStatus, errorThrown) {
                                    Swal.fire("Failed !", textStatus, "error");
                                });
                            }
                        })

                        e.element.find('.dx-datagrid-header-panel').append($customButton)
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
                        allowAdding: true,
                        allowUpdating: true,
                        allowDeleting: true,
                        useIcons: true,
                        form: {
                            itemType: "group",
                            caption: "Loading",
                            items: [
                                {
                                    dataField: "transaction_number"
                                },
                                {
                                    dataField: "detail_location_id"
                                },
                                {
                                    dataField: "quantity"
                                },
                                {
                                    dataField: "uom_id"
                                },
                                {
                                    dataField: "reference_number"
                                },
                                {
                                    dataField: "start_datetime"
                                },
                                {
                                    dataField: "end_datetime"
                                },
                                {
                                    dataField: "equipment_id"
                                },
                                {
                                    dataField: "hour_usage"
                                },
                                {
                                    dataField: "survey_id"
                                },
                                {
                                    dataField: "final_quantity"
                                },
                                {
                                    dataField: "note"
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
                    onInitNewRow: function (e) {
                        e.data.shipping_transaction_id = currentRecord.id;
                    },
                    onEditorPreparing: function (e) {
                        if (e.dataField == "note" && e.parentType === "dataRow") {
                            const defaultValueChangeHandler = e.editorOptions.onValueChanged;
                            e.editorName = "dxTextArea"; // Change the editor's type
                            e.editorOptions.onValueChanged = function (args) {  // Override the default handler
                                // ...
                                // Custom commands go here
                                // ...
                                // If you want to modify the editor value, call the setValue function:
                                // e.setValue(newValue);
                                // Otherwise, call the default handler:
                                defaultValueChangeHandler(args);
                            }
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
                                saveAs(new Blob([buffer], { type: 'application/octet-stream' }), detailName + '.xlsx');
                            });
                        });
                        e.cancel = true;
                    }
                });
        }
    }

    function createQualitySamplingTab(masterDetailData) {
        return function () {
            let currentRecord = masterDetailData;
            let detailName = "Shipping";
            let urlDetail = "/api/" + areaName + "/" + detailName;
            bargingTransactionData = currentRecord

            documentDataGrid = $("<div>")
                .dxDataGrid({
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "id",
                        loadUrl: urlDetail + "/ByShippingTransactionId/" + encodeURIComponent(currentRecord.id),
                        //updateUrl: urlDetail + "/UpdateData",
                        //deleteUrl: urlDetail + "/DeleteData",
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

    let documentDataGrid
    function createDocumentsTabTemplate(masterDetailData) {
        return function () {
            let currentRecord = masterDetailData;
            let detailName = "ShippingLoadUnloadDocument";
            let urlDetail = "/api/" + areaName + "/" + detailName;
            shippingTransactionData = currentRecord

            documentDataGrid = $("<div>")
                .dxDataGrid({
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "id",
                        loadUrl: urlDetail + "/ByShippingTransactionId/" + encodeURIComponent(currentRecord.id),
                        updateUrl: urlDetail + "/Loading/UpdateData",
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
                        //    dataField: "document_type_id",
                        //    caption: "Document Type",
                        //    visible: true,
                        //    lookup: {
                        //        dataSource: function () {
                        //            return {
                        //                store: DevExpress.data.AspNet.createStore({
                        //                    key: "value",
                        //                    loadUrl: "/api/General/MasterList/MasterListIdLookup",
                        //                    onBeforeSend: function (method, ajaxOptions) {
                        //                        ajaxOptions.xhrFields = { withCredentials: true };
                        //                        ajaxOptions.beforeSend = function (request) {
                        //                            request.setRequestHeader("Authorization", "Bearer " + token);
                        //                        };
                        //                    }
                        //                }),
                        //                filter: ["item_group", "=", "document-type"]
                        //            }
                        //        },
                        //        searchEnabled: true,
                        //        valueExpr: "value",
                        //        displayExpr: "text"
                        //    },
                        //},
                        {
                            dataField: "activity_id",
                            dataType: "string",
                            caption: "Activity",
                            lookup: {
                                dataSource: function () {
                                    return {
                                        store: DevExpress.data.AspNet.createStore({
                                            key: "value",
                                            loadUrl: "/api/General/MasterList/MasterListIdLookup",
                                            onBeforeSend: function (method, ajaxOptions) {
                                                ajaxOptions.xhrFields = { withCredentials: true };
                                                ajaxOptions.beforeSend = function (request) {
                                                    request.setRequestHeader("Authorization", "Bearer " + token);
                                                };
                                            }
                                        }),
                                        filter: ["item_group", "=", "activity"]
                                    }
                                },
                                searchEnabled: true,
                                valueExpr: "value",
                                displayExpr: "text"
                            },
                        },
                        {
                            dataField: "remark",
                            dataType: "string",
                            caption: "Remark",
                        },
                        //{
                        //    dataField: "quantity",
                        //    dataType: "boolean",
                        //    caption: "Quantity"
                        //},
                        //{
                        //    dataField: "quality",
                        //    dataType: "boolean",
                        //    caption: "Quality"
                        //},
                        {
                            dataField: "filename",
                            dataType: "string",
                            caption: "Document"
                        },
                        {
                            caption: "Download",
                            type: "buttons",
                            width: 100,
                            buttons: [{
                                cssClass: "btn-dxdatagrid",
                                hint: "Download attachment",
                                text: "Download",
                                onClick: function (e) {
                                    // Download file from Ajax. Ref: https://stackoverflow.com/a/9970672
                                    let documentData = e.row.data
                                    let documentName = /[^\\]*$/.exec(documentData.filename)[0]

                                    let xhr = new XMLHttpRequest()
                                    xhr.open("GET", "/api/Port/ShippingLoadUnloadDocument/DownloadDocument/" + documentData.id, true)
                                    xhr.responseType = "blob"
                                    xhr.setRequestHeader("Authorization", "Bearer " + token)

                                    xhr.onload = function (e) {
                                        let blobURL = window.webkitURL.createObjectURL(xhr.response)

                                        let a = document.createElement("a")
                                        a.href = blobURL
                                        a.download = documentName
                                        document.body.appendChild(a)
                                        a.click()
                                    };

                                    xhr.send()
                                }
                            }]
                        },
                        {
                            type: "buttons",
                            buttons: ["edit", "delete"]
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
                        allowAdding: true,
                        allowUpdating: false,
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

    const documentPopupOptions = {
        width: "80%",
        height: "auto",
        showTitle: true,
        title: "Add Attachment",
        visible: false,
        dragEnabled: false,
        closeOnOutsideClick: true,
        contentTemplate: function (e) {
            let formContainer = $("<div>")
            formContainer.dxForm({
                formData: {
                    id: "",
                    shipping_transaction_id: shippingTransactionData.id,
                    activity_id: "",
                    document_type_id: "",
                    remark: "",
                    quantity: false,
                    quality: false,
                    file: ""
                },
                colCount: 2,
                items: [
                    {
                        dataField: "shipping_transaction_id",
                        label: {
                            text: "Shipping Transaction Id"
                        },
                        validationRules: [{
                            type: "required"
                        }],
                        visible: false
                    },
                    {
                        dataField: "activity_id",
                        editorType: "dxSelectBox",
                        label: {
                            text: "Activity"
                        },
                        editorOptions: {
                            dataSource: new DevExpress.data.DataSource({
                                store: DevExpress.data.AspNet.createStore({
                                    key: "value",
                                    loadUrl: "/api/General/MasterList/MasterListIdLookup",
                                    onBeforeSend: function (method, ajaxOptions) {
                                        ajaxOptions.xhrFields = { withCredentials: true };
                                        ajaxOptions.beforeSend = function (request) {
                                            request.setRequestHeader("Authorization", "Bearer " + token);
                                        };
                                    }
                                }),
                                filter: ["item_group", "=", "activity"]
                            }),
                            searchEnabled: true,
                            valueExpr: "value",
                            displayExpr: "text"
                        },
                    },
                    //{
                    //    dataField: "document_type_id",
                    //    editorType: "dxSelectBox",
                    //    label: {
                    //        text: "Document Type"
                    //    },
                    //    editorOptions: {
                    //        dataSource: new DevExpress.data.DataSource({
                    //            store: DevExpress.data.AspNet.createStore({
                    //                key: "value",
                    //                loadUrl: "/api/General/MasterList/MasterListIdLookup",
                    //                onBeforeSend: function (method, ajaxOptions) {
                    //                    ajaxOptions.xhrFields = { withCredentials: true };
                    //                    ajaxOptions.beforeSend = function (request) {
                    //                        request.setRequestHeader("Authorization", "Bearer " + token);
                    //                    };
                    //                }
                    //            }),
                    //            filter: ["item_group", "=", "document-type"]
                    //        }),
                    //        searchEnabled: true,
                    //        valueExpr: "value",
                    //        displayExpr: "text"
                    //    },
                    //},
                    //{
                    //    dataField: "quantity",
                    //    editorType: "dxCheckBox",
                    //    label: {
                    //        text: "Quantity"
                    //    },
                    //},
                    //{
                    //    dataField: "quality",
                    //    editorType: "dxCheckBox",
                    //    label: {
                    //        text: "Quality"
                    //    },
                    //},
                    {
                        dataField: "remark",
                        editortype: "dxTextArea",
                        label: {
                            text: "Remark"
                        },
                        editorOptions: {
                            height: 50
                        },
                        colSpan: 2
                    },
                    {
                        dataField: "file",
                        name: "file",
                        label: {
                            text: "File"
                        },
                        template: function (data, itemElement) {
                            itemElement.append($("<div>").attr("id", "file").dxFileUploader({
                                uploadMode: "useForm",
                                multiple: false,
                                maxFileSize: maxFileSize,
                                invalidMaxFileSizeMessage: "Max. file size is 50 Mb",
                                onValueChanged: function (e) {
                                    data.component.updateData(data.dataField, e.value)
                                }
                            }));
                        },
                        validationRules: [{
                            type: "required"
                        }],
                        colSpan: 2
                    },
                    {
                        itemType: "button",
                        colSpan: 2,
                        horizontalAlignment: "right",
                        buttonOptions: {
                            text: "Save",
                            type: "secondary",
                            useSubmitBehavior: true,
                            onClick: function () {
                                let formData = formContainer.dxForm("instance").option('formData')
                                let file = formData.file[0]

                                var reader = new FileReader();
                                reader.readAsDataURL(file);
                                reader.onload = function () {
                                    let fileName = file.name
                                    let fileSize = file.size
                                    let data = reader.result.split(',')[1]

                                    if (fileSize >= maxFileSize) {
                                        return;
                                    }

                                    let newFormData = {
                                        "shippingTransId": formData.shipping_transaction_id,
                                        "activityId": formData.activity_id,
                                        "documentTypeId": formData.document_type_id,
                                        "remark": formData.remark,
                                        "quantity": formData.quantity,
                                        "quality": formData.quality,
                                        "fileName": fileName,
                                        "fileSize": fileSize,
                                        "data": data
                                    }

                                    /*console.log(newFormData)*/

                                    $.ajax({
                                        url: `/api/${areaName}/ShippingLoadUnloadDocument/InsertData`,
                                        data: JSON.stringify(newFormData),
                                        type: "POST",
                                        contentType: "application/json",
                                        beforeSend: function (xhr) {
                                            xhr.setRequestHeader("Authorization", "Bearer " + token);
                                        },
                                        success: function (response) {
                                            documentPopup.hide()
                                            documentDataGrid.dxDataGrid("instance").refresh()
                                        }
                                    })
                                }
                            }
                        }
                    }
                ]
            })
            e.append(formContainer)
        }
    }

    const documentPopup = $("<div>")
        .dxPopup(documentPopupOptions).appendTo("body").dxPopup("instance")

    const openDocumentPopup = function () {
        documentPopup.option("contentTemplate", documentPopupOptions.contentTemplate.bind(this));
        documentPopup.show()
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
                url: "/api/Port/Shipping/Loading/UploadDocument",
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