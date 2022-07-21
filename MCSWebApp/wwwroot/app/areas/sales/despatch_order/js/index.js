$(function () {

    var token = $.cookie("Token");
    var areaName = "Sales";
    var entityName = "DespatchOrder";
    var url = "/api/" + areaName + "/" + entityName;
    let is_vessel_geared = false;
    var despatchOrderData = null;

    //function formatTanggal(x) {
    //    theDate = new Date(x);
    //    formatted_date = theDate.getFullYear() + "-" + (theDate.getMonth() + 1).toString().padStart(2, "0")
    //        + "-" + theDate.getDate().toString().padStart(2, "0") + " " + theDate.getHours().toString().padStart(2, "0")
    //        + ":" + theDate.getMinutes().toString().padStart(2, "0");
    //    return formatted_date;
    //}

    //var tgl1 = sessionStorage.getItem("DespacthOrderDate1");
    //var tgl2 = sessionStorage.getItem("DespacthOrderDate2");

    //var date = new Date(), y = date.getFullYear(), m = date.getMonth();
    //var firstDay = new Date(y, m, 1);
    //var lastDay = new Date(y, m + 1, 0);

    //if (tgl1 != null)
    //    firstDay = Date.parse(tgl1);

    //if (tgl2 != null)
    //    lastDay = Date.parse(tgl2);

    //$("#date-box1").dxDateBox({
    //    type: "date",
    //    value: firstDay,
    //    onValueChanged: function (data) {
    //        firstDay = new Date(data.value);
    //        sessionStorage.setItem("DespacthOrderDate1", formatTanggal(firstDay));
    //        _loadUrl = url + "/DataGrid/" + encodeURIComponent(formatTanggal(firstDay))
    //            + "/" + encodeURIComponent(formatTanggal(lastDay));
    //    }
    //});

    //$("#date-box2").dxDateBox({
    //    type: "date",
    //    value: lastDay,
    //    onValueChanged: function (data) {
    //        lastDay = new Date(data.value);
    //        sessionStorage.setItem("DespacthOrderDate2", formatTanggal(lastDay));
    //        _loadUrl = url + "/DataGrid/" + encodeURIComponent(formatTanggal(firstDay))
    //            + "/" + encodeURIComponent(formatTanggal(lastDay));
    //    }
    //});

    //$('#btnView').on('click', function () {
    //    location.reload();
    //})

    //var _loadUrl = url + "/DataGrid/" + encodeURIComponent(formatTanggal(firstDay))
    //    + "/" + encodeURIComponent(formatTanggal(lastDay));

    var grid = $("#grid").dxDataGrid({
        dataSource: DevExpress.data.AspNet.createStore({
            key: "id",
            loadUrl: url + "/DataGrid",
            //loadUrl: _loadUrl,
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
                dataField: "despatch_plan_id",
                dataType: "string",
                caption: "Shipment Plan",
                lookup: {
                    dataSource: function (options) {
                        return {
                            store: DevExpress.data.AspNet.createStore({
                                key: "value",
                                loadUrl: "/api/Planning/ShipmentPlan/ShipmentPlanIdLookup",
                                onBeforeSend: function (method, ajaxOptions) {
                                    ajaxOptions.xhrFields = { withCredentials: true };
                                    ajaxOptions.beforeSend = function (request) {
                                        request.setRequestHeader("Authorization", "Bearer " + token);
                                    };
                                }
                            }),
                            //filter: options.data ? ["sales_contract_term_id", "=", options.data.contract_term_id] : null
                        }
                    },
                    valueExpr: "value",
                    displayExpr: "text"
                },
                setCellValue: function (rowData, value) {
                    rowData.despatch_plan_id = value
                    rowData.sales_contract_id = null
                    rowData.contract_term_id = null
                    //rowData.required_quantity = null
                    //rowData.is_geared = null
                    //rowData.seller_id = null
                    //rowData.customer_id = null
                    //rowData.ship_to = null
                    //rowData.contract_product_id = null
                    //rowData.uom_id = null
                    //rowData.turn_time = null
                    //rowData.despatch_demurrage_term_rate = null
                    //rowData.despatch_percentage = null
                    //rowData.loading_rate = null
                },
                calculateSortValue: function (data) {
                    var value = this.calculateCellValue(data);
                    return this.lookup.calculateCellValue(value);
                },
            },            
            {
                dataField: "despatch_order_number",
                dataType: "string",
                caption: "Despatch Order Number",
                sortOrder: "asc",
                placeholder: "[auto generate]",
                allowEditing: false
            },
            {
                //dataField: "order_reference_date",
                dataField: "despatch_order_date",
                dataType: "date",
                caption: "Despatch Order Date",
            },
            {
                dataField: "laycan_start",
                dataType: "date",
                caption: "Laycan Start",
                visible: false,
                //setCellValue: (rowData, value) => {
                //    rowData.laycan_start = value

                //    var laycanEndDate = new Date(value)
                //    laycanEndDate.setDate(laycanEndDate.getDate() + 9)
                //    rowData.laycan_end = laycanEndDate
                //}
            },
            {
                dataField: "laycan_end",
                dataType: "date",
                caption: "Laycan End",
                //allowEditing: false,
                visible: false
            },
            {
                dataField: "eta_plan",
                dataType: "datetime",
                caption: "ETA",
                visible: false
            },
            //{
            //    dataField: "bill_of_lading_date",
            //    dataType: "date",
            //    caption: "Bill of Lading Date",
            //    visible: false,
            //    validationRules: [{
            //        type: "required",
            //        message: "The field is required."
            //    }]
            //},
            {
                dataField: "vessel_id",
                dataType: "string",
                caption: "Transport",
                lookup: {
                    dataSource: function (options) {
                        return {
                            store: DevExpress.data.AspNet.createStore({
                                key: "value",
                                loadUrl: "/api/Transport/Vessel/VesselBargeIdLookup",
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
                setCellValue: function (rowData, value) {
                    rowData.vessel_id = value
                    rowData.contract_term_id = null
                    rowData.contract_product_id = null
                    rowData.turn_time = null
                    rowData.despatch_demurrage_rate = null
                    rowData.despatch_percentage = null
                    rowData.loading_rate = null
                    rowData.laytime_duration = null
                    rowData.laytime_text = null

                },
                calculateSortValue: function (data) {
                    var value = this.calculateCellValue(data);
                    return this.lookup.calculateCellValue(value);
                }
            },
            {
                dataField: "sales_contract_id",
                dataType: "string",
                caption: "Sales Contract",
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
                    rowData.contract_term_id = null
                    rowData.contract_product_id = null
                    rowData.turn_time = null
                    rowData.despatch_demurrage_rate = null
                    rowData.despatch_percentage = null
                    rowData.loading_rate = null
                    rowData.laytime_duration = null
                    rowData.laytime_text = null
                },
                calculateSortValue: function (data) {
                    var value = this.calculateCellValue(data);
                    return this.lookup.calculateCellValue(value);
                },
                visible: false
            },
            {
                dataField: "contract_term_id",
                dataType: "string",
                caption: "Contract Term",
                lookup: {
                    dataSource: function (options) {
                        return {
                            store: DevExpress.data.AspNet.createStore({
                                key: "value",
                                loadUrl: "/api/Sales/SalesContractTerm/SalesContractTermIdLookup",
                                onBeforeSend: function (method, ajaxOptions) {
                                    ajaxOptions.xhrFields = { withCredentials: true };
                                    ajaxOptions.beforeSend = function (request) {
                                        request.setRequestHeader("Authorization", "Bearer " + token);
                                    };
                                }
                            }),
                            filter: options.data ? ["sales_contract_id", "=", options.data.sales_contract_id] : null
                        }
                    },
                    valueExpr: "value",
                    displayExpr: "text"
                },
                setCellValue: function (rowData, value) {
                    rowData.contract_term_id = value

                    //rowData.despatch_plan_id = null

                    //rowData.contract_product_id = null

                    //rowData.uom_id = null
                    //rowData.delivery_term_id = null
                },
                calculateSortValue: function (data) {
                    var value = this.calculateCellValue(data);
                    return this.lookup.calculateCellValue(value);
                },
            },
            {
                dataField: "required_quantity",
                dataType: "number",
                caption: "Cargo Quantity"
            },
            {
                dataField: "turn_time",
                dataType: "number",
                caption: "Turn Time",
                visible: false,
                allowEditing: false
            },
            {
                dataField: "loading_rate",
                dataType: "number",
                caption: "Loading Rate",
                visible: false,
                allowEditing: true,
                onKeyDown: function (e) {
                    console.log(e);
                    if (e.name === "changedProperty") {
                        // handle the property change here
                    }
                }
            },
            {
                dataField: "despatch_demurrage_rate",
                dataType: "number",
                caption: "Despatch Demurrage Rate",
                visible: false,
                allowEditing: true
            },
            {
                dataField: "despatch_percentage",
                dataType: "number",
                caption: "Despatch (%)",
                visible: false,
                allowEditing: false
            },
            {
                dataField: "currency_code",
                dataType: "string",
                caption: "Currency",
                visible: false,
                allowEditing: false
            },
            {
                dataField: "laytime_text",
                dataType: "string",
                caption: "Laytime Allowed",
                placeholder: "[auto calculate]",
                visible: false,
                allowEditing: false,
            },
            {
                dataField: "laytime_duration",
                dataType: "number",
                caption: "s",
                visible: false,
                allowEditing: false
            },
            {
                dataField: "seller_id",
                dataType: "string",
                caption: "Seller",
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: "/api/SystemAdministration/Organization/ParentOrganizationIdLookup",
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
                dataField: "customer_id",
                dataType: "string",
                caption: "Buyer",
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
                dataField: "ship_to",
                dataType: "string",
                caption: "Ship To (End User)",
                lookup: {
                    dataSource: function (options) {
                        var _url = url + "/EndUserIdLookup";

                        if (options !== undefined && options !== null) {
                            if (options.data !== undefined && options.data !== null) {
                                if (options.data.sales_contract_id !== undefined
                                    && options.data.sales_contract_id !== null) {
                                    _url += "?SalesContractId=" + encodeURIComponent(options.data.sales_contract_id);
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
                allowEditing: true
            },
            {
                dataField: "contract_product_id",
                dataType: "string",
                caption: "Contract Product",
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: "/api/Sales/SalesContractProduct/SalesContractProductIdLookup",
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
                dataField: "product_name",
                dataType: "string",
                caption: "Product Name",
                visible: false,
                allowEditing: false
            },
            {
                dataField: "final_quantity",
                dataType: "number",
                caption: "Final Quantity",
                visible: false
            },
            {
                dataField: "uom_id",
                dataType: "string",
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
                },
                calculateSortValue: function (data) {
                    var value = this.calculateCellValue(data);
                    return this.lookup.calculateCellValue(value);
                },
                visible: false,
                allowEditing: false
            },
            {
                dataField: "fulfilment_type_id",
                dataType: "string",
                caption: "Despatch Fulfilment Type",
                visible: false,
                lookup: {
                    dataSource: function (options) {
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
                            filter: ["item_group", "=", "fulfilment-type"]
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
                dataField: "multiple_barge",
                dataType: "boolean",
                caption: "FOB Barge(Multiple Barges)",
                visible: false
            },
            {
                dataField: "laycan_committed",
                dataType: "boolean",
                caption: "Laycan Committed",
                visible: false
            },
            
            {
                dataField: "edit_loading_rate",
                dataType: "boolean",
                caption: "Edit Loading Rate",
                visible: false,
                setCellValue: function (newData, value) {
                    newData.edit_loading_rate = value;
                }
            },
            {
                dataField: "edit_despatch_rate",
                dataType: "boolean",
                caption: "Edit Despatch Demurrage Rate",
                visible: false,
                setCellValue: function (newData, value) {
                    newData.edit_despatch_rate = value;
                }
            },
            {
                dataField: "vessel_committed",
                dataType: "boolean",
                caption: "Vessel Committed",
                visible: false
            },
            {
                dataField: "eta_committed",
                dataType: "boolean",
                caption: "ETA Committed",
                visible: false
            },
            {
                dataField: "is_geared",
                dataType: "boolean",
                caption: "Is Geared",
                visible: false,
                allowEditing: false
            },
            {
                dataField: "loading_port",
                dataType: "string",
                caption: "Loading Port",
                visible: false
            },
            //{
            //    dataField: "port_location_id",
            //    dataType: "string",
            //    caption: "Loading Port",
            //    lookup: {
            //        dataSource: DevExpress.data.AspNet.createStore({
            //            key: "value",
            //            loadUrl: "/api/Location/PortLocation/PortLocationIdLookup",
            //            onBeforeSend: function (method, ajaxOptions) {
            //                ajaxOptions.xhrFields = { withCredentials: true };
            //                ajaxOptions.beforeSend = function (request) {
            //                    request.setRequestHeader("Authorization", "Bearer " + token);
            //                };
            //            }
            //        }),
            //        valueExpr: "value",
            //        displayExpr: "text"
            //    },
            //    calculateSortValue: function (data) {
            //        var value = this.calculateCellValue(data);
            //        return this.lookup.calculateCellValue(value);
            //    },
            //    visible: false,
            //    allowEditing: true
            //},
            {
                dataField: "discharge_port",
                dataType: "string",
                caption: "Discharge Port",
                visible: false
            },
            {
                dataField: "delivery_term_id",
                dataType: "string",
                caption: "Delivery Term",
                visible: false,
                allowEditing: false,
                lookup: {
                    dataSource: function (options) {
                        return {
                            store: DevExpress.data.AspNet.createStore({
                                key: "value",
                                loadUrl: "/api/General/MasterList/MasterListIdLookup",
                                //loadUrl: "/api/Planning/ShipmentPlan/ShipmentPlanIdLookup",
                                onBeforeSend: function (method, ajaxOptions) {
                                    ajaxOptions.xhrFields = { withCredentials: true };
                                    ajaxOptions.beforeSend = function (request) {
                                        request.setRequestHeader("Authorization", "Bearer " + token);
                                    };
                                }
                            }),
                            filter: ["item_group", "=", "delivery-term"]
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
            //{
            //    dataField: "document_reference_id",
            //    dataType: "string",
            //    caption: "Document Reference",
            //    visible: false,
            //    lookup: {
            //        dataSource: function (options) {
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
            //        valueExpr: "value",
            //        displayExpr: "text"
            //    },
            //    calculateSortValue: function (data) {
            //        var value = this.calculateCellValue(data);
            //        return this.lookup.calculateCellValue(value);
            //    },
            //},
            {
                dataField: "letter_of_credit",
                dataType: "string",
                caption: "Letter of Credit",
                visible: false
            },
            {
                dataField: "surveyor_id",
                dataType: "string",
                caption: "Surveyor",
                visible: false,
                lookup: {
                    dataSource: function (options) {
                        return {
                            store: DevExpress.data.AspNet.createStore({
                                key: "value",
                                loadUrl: "/api/Organisation/Contractor/ContractorIdLookupByIsSurveyor?isSurveyor=true",
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
                calculateSortValue: function (data) {
                    var value = this.calculateCellValue(data);
                    return this.lookup.calculateCellValue(value);
                },
            },
            {
                dataField: "shipping_agent",
                dataType: "string",
                caption: "Shipping Agent",
                visible: false
            },
            {
                dataField: "notes",
                dataType: "string",
                caption: "Notes",
                visible: false
            },
            {
                dataField: "credit_limit_alert",
                dataType: "string",
                caption: "Credit Limit Alert",
                visible: false,
                allowEditing: false,
                validationRules: [
                    {
                        type: "custom",
                        message: "Remain credit limit is insufficient",
                        validationCallback: function (args) {
                            var color = args.data.credit_limit_color
                            if (color == 'red') {
                                args.rule.message = "Remain credit limit is insufficient"
                                return false;
                            }
                            return true;
                        },
                        reevaluate: true
                    }
                ],
            },
            //{
            //    dataField: "quantity",
            //    dataType: "number",
            //    caption: "Draft Survey ##",
            //    visible: false
            //},
            {
                dataField: "credit_limit_color",
                dataType: "string",
                visible: false,
                allowEditing: false,
            },
            {
                dataField: "created_on",
                caption: "Created On",
                dataType: "string",
                visible: false,
                sortOrder: "desc"
            },
            {
                caption: "Initial Info",
                type: "buttons",
                width: 110,
                buttons: [{
                    cssClass: "btn-dxdatagrid",
                    hint: "Publish Initial Information",
                    text: "Publish",
                    onClick: function (e) {
                        despatchOrderData = e.row.data
                        showInitialInformationPopup()
                    }
                }]
            },
            {
                type: "buttons",
                buttons: ["edit", "delete"]
            }
        ],
        masterDetail: {
            enabled: true,
            template: function (container, options) {
                var currentRecord = options.data;
                //console.log(currentRecord)
                despatchOrderData = currentRecord;

                // Despatch Order Information container
                renderDespatchOrderInformation(currentRecord, container)

                // Despatch Order Product Specification
                renderDespatchOrderProductSpecification(currentRecord, container)

                // Despatch Order Delay Definition - Not Used anymore (moved to sales contract term despatch/demurrage term)
                /*renderDespatchOrderDelayDefinition(currentRecord, container)*/

                renderDespatchOrderDocument(currentRecord, container)
                renderTranshipment(currentRecord, container)
            }
        },
        onEditorPreparing: function (e) {
            // Set onValueChanged for sales_contract_id
            if (e.parentType === "dataRow" && e.dataField == "sales_contract_id") {

                let standardHandler = e.editorOptions.onValueChanged
                let index = e.row.rowIndex
                let grid = e.component

                e.editorOptions.onValueChanged = function (e) { // Overiding the standard handler

                    // Get its value (Id) on value changed
                    let salesContract = e.value

                    // Get another data from API after getting the Id
                    $.ajax({
                        url: '/api/Sales/SalesContract/DataDetail?Id=' + salesContract,
                        type: 'GET',
                        contentType: "application/json",
                        beforeSend: function (xhr) {
                            xhr.setRequestHeader("Authorization", "Bearer " + token);
                        },
                        success: function (response) {
                            let salesContractData = response.data[0]

                            //console.log("SalesContract/DataDetail: ", response);

                            // Set its corresponded field's value
                            grid.cellValue(index, "customer_id", salesContractData.customer_id)
                            grid.cellValue(index, "seller_id", salesContractData.seller_id)
                            //grid.cellValue(index, "ship_to", salesContractData.end_user_id)


                            $.ajax({
                                url: '/api/Sales/Customer/CreditLimitAlert?customer_id=' + salesContractData.customer_id,
                                type: 'GET',
                                contentType: "application/json",
                                beforeSend: function (xhr) {
                                    xhr.setRequestHeader("Authorization", "Bearer " + token);
                                },
                                success: function (response) {
                                    let creditLimitAlert = response[0]

                                    // Set its corresponded field's value
                                    grid.cellValue(index, "credit_limit_alert", creditLimitAlert.persentase + '%')
                                    grid.cellValue(index, "credit_limit_color", creditLimitAlert.color)
                                }
                            })

                        }
                    })

                    standardHandler(e) // Calling the standard handler to save the edited value
                }
            }

            // Set onValueChanged for contract_term_id
            if (e.parentType === "dataRow" && e.dataField == "contract_term_id") {

                let standardHandler = e.editorOptions.onValueChanged
                let index = e.row.rowIndex
                let grid = e.component
                var currentRecord = e.row.data;

                e.editorOptions.onValueChanged = function (e) { // Overiding the standard handler
                    // Get its value (Id) on value changed
                    let salesContractTermId = e.value

                    // Get another data from API after getting the Id
                    $.ajax({
                        url: '/api/Sales/SalesContractTerm/DataDetail?Id=' + salesContractTermId,
                        type: 'GET',
                        contentType: "application/json",
                        beforeSend: function (xhr) {
                            xhr.setRequestHeader("Authorization", "Bearer " + token);
                        },
                        success: function (response) {
                            let salesContractTermData = response.data[0];
                            console.log("salesContractTermData", salesContractTermData)

                            console.log("grid", grid)

                            grid.cellValue(index, "contract_product_id", salesContractTermData.sales_contract_product_id);
                            grid.cellValue(index, "uom_id", salesContractTermData.uom_id);
                            grid.cellValue(index, "currency_code", salesContractTermData.currency_code);

                            grid.cellValue(index, "turn_time", salesContractTermData.turn_time)
                            grid.cellValue(index, "despatch_demurrage_rate", salesContractTermData.despatch_demurrage_term_rate)
                            grid.cellValue(index, "despatch_percentage", salesContractTermData.despatch_percentage)


                            grid.cellValue(index, "loading_rate", null)
                            if (currentRecord.is_geared && currentRecord.is_geared != null) {
                                grid.cellValue(index, "loading_rate", salesContractTermData.loading_rate_geared)
                            } else if (!currentRecord.is_geared && currentRecord.is_geared != null) {
                                grid.cellValue(index, "loading_rate", salesContractTermData.loading_rate_gearless)
                            }

                            grid.cellValue(index, "laytime_duration", null);
                            grid.cellValue(index, "laytime_text", null);
                            if (currentRecord.is_geared != null) {
                                var res = calculateLaytimeAllowed(currentRecord.required_quantity, currentRecord.is_geared ? salesContractTermData.loading_rate_geared : salesContractTermData.loading_rate_gearless);
                                grid.cellValue(index, "laytime_duration", res.value);
                                grid.cellValue(index, "laytime_text", res.text);
                            }
                            

                            //-- Get Sales Contract Product
                            if (salesContractTermData.sales_contract_product_id != "" && salesContractTermData.sales_contract_product_id != null) {
                                $.ajax({
                                    url: '/api/Sales/SalesContractProduct/DataDetail?Id=' + salesContractTermData.sales_contract_product_id,
                                    type: 'GET',
                                    contentType: "application/json",
                                    beforeSend: function (xhr) {
                                        xhr.setRequestHeader("Authorization", "Bearer " + token);
                                    },
                                    success: function (response) {
                                        let salesContractProductData = response.data[0];
                                        if (salesContractProductData != null) {
                                            grid.cellValue(index, "product_name", salesContractProductData.product_name);
                                        }
                                    }
                                })
                            }
                        }
                    })

                    standardHandler(e) // Calling the standard handler to save the edited value
                }
            }

            // Set onValueChanged for despatch_plan_id
            if (e.parentType === "dataRow" && e.dataField == "despatch_plan_id") {

                let standardHandler = e.editorOptions.onValueChanged
                let index = e.row.rowIndex
                let grid = e.component

                var currentRecord = e.row.data;

                //var selectedRowIndex = e.component.getRowIndexByKey(e.selectedRowKeys[0]);

                e.editorOptions.onValueChanged = function (e) { // Overiding the standard handler

                    // Get its value (Id) on value changed
                    let salesContractDespatchPlanId = e.value
                    // Get another data from API after getting the Id
                    $.ajax({
                        url: '/api/Planning/ShipmentPlan/ShipmentPlanByID?Id=' + salesContractDespatchPlanId,
                        type: 'GET',
                        contentType: "application/json",
                        beforeSend: function (xhr) {
                            xhr.setRequestHeader("Authorization", "Bearer " + token);
                        },
                        success: function (response) {
                            let shipmentPlanData = response.data[0]
                            console.log("shipmentPlanData: ", shipmentPlanData)
                            // Set its corresponded field's value


                            is_vessel_geared = shipmentPlanData.is_geared;
                            grid.cellValue(index, "is_geared", shipmentPlanData.is_geared);
                            grid.cellValue(index, "required_quantity", shipmentPlanData.qty_sp);
                            grid.cellValue(index, "sales_contract_id", shipmentPlanData.sales_contract_id);
                            grid.cellValue(index, "vessel_id", shipmentPlanData.transport_id);
                            grid.cellValue(index, "eta_plan", shipmentPlanData.eta);
                            grid.cellValue(index, "delivery_term_id", shipmentPlanData.incoterm);

                            $.ajax({
                                url: '/api/Sales/SalesContract/DataDetail?Id=' + shipmentPlanData.sales_contract_id,
                                type: 'GET',
                                contentType: "application/json",
                                beforeSend: function (xhr) {
                                    xhr.setRequestHeader("Authorization", "Bearer " + token);
                                },
                                success: function (response) {
                                    let salesContractData = response.data[0]
                                    //console.log("SalesContract/DataDetail: ", response);
                                    grid.cellValue(index, "customer_id", salesContractData.customer_id)
                                    grid.cellValue(index, "seller_id", salesContractData.seller_id)
                                    //grid.cellValue(index, "ship_to", salesContractData.end_user_id)

                                    $.ajax({
                                        url: '/api/Sales/Customer/CreditLimitAlert?customer_id=' + salesContractData.customer_id,
                                        type: 'GET',
                                        contentType: "application/json",
                                        beforeSend: function (xhr) {
                                            xhr.setRequestHeader("Authorization", "Bearer " + token);
                                        },
                                        success: function (response) {
                                            let creditLimitAlert = response[0]

                                            // Set its corresponded field's value
                                            grid.cellValue(index, "credit_limit_alert", creditLimitAlert.persentase + '%')
                                            grid.cellValue(index, "credit_limit_color", creditLimitAlert.color)
                                        }
                                    })

                                }
                            })
                        }
                    })

                    standardHandler(e) // Calling the standard handler to save the edited value
                }
            }

            if (e.parentType === "dataRow" && e.dataField == "vessel_id") {
                let standardHandler = e.editorOptions.onValueChanged

                let index = e.row.rowIndex
                let grid = e.component

                //let vesselId = e.value
                var currentRecord = e.row.data;
                //console.log("vesselId: ", vesselId)
                //console.log("currentRecord: ", currentRecord)

                //var selectedRowIndex = e.component.getRowIndexByKey(e.selectedRowKeys[0]);

                e.editorOptions.onValueChanged = function (e) { // Overiding the standard handler
                    let vesselId = e.value
                    //console.log("vesselId: ", vesselId)
                    //console.log("currentRecord: ", currentRecord)
                    // Get its value (Id) on value changed
                    let salesContractDespatchPlanId = e.value
                    // Get another data from API after getting the Id
                    $.ajax({
                        url: '/api/Transport/Vessel/Detail/' + vesselId,
                        type: 'GET',
                        contentType: "application/json",
                        beforeSend: function (xhr) {
                            xhr.setRequestHeader("Authorization", "Bearer " + token);
                        },
                        success: function (response) {
                            if (response) {
                                grid.cellValue(index, "is_geared", response.is_geared);
                            } else {
                                grid.cellValue(index, "is_geared", false);
                            }
                        }
                    })

                    standardHandler(e) // Calling the standard handler to save the edited value
                }
            }

            if (e.parentType === "dataRow" && e.dataField == "laycan_start") {
                e.editorOptions.disabled = e.row.data.laycan_committed;

                let standardHandler = e.editorOptions.onValueChanged

                let index = e.row.rowIndex
                let grid = e.component

                e.editorOptions.onValueChanged = function (e) { // Overiding the standard handler

                    let laycanStart = e.value
                    //console.log("laycanStart: ", laycanStart)

                    var laycanEndDate = new Date(laycanStart)
                    laycanEndDate.setDate(laycanEndDate.getDate() + 9)
                    grid.cellValue(index, "laycan_start", e.value);
                    grid.cellValue(index, "laycan_end", laycanEndDate);

                    standardHandler(e) // Calling the standard handler to save the edited value
                }
            }
            if (e.parentType === "dataRow" && e.dataField == "laycan_end") {
                e.editorOptions.disabled = e.row.data.laycan_committed;
            }

            if (e.parentType === "dataRow" && e.dataField == "eta_plan") {
                e.editorOptions.disabled = e.row.data.eta_committed;
            }
            if (e.parentType === "dataRow" && e.dataField == "vessel_id") {
                e.editorOptions.disabled = e.row.data.vessel_committed;
            }
            
            if (e.parentType === "dataRow" && e.dataField == "despatch_demurrage_rate") {
                e.editorOptions.disabled = !e.row.data.edit_despatch_rate;
            }

            if (e.parentType === "dataRow" && e.dataField === "required_quantity") {
                e.editorOptions.onValueChanged = args => {

                    let index = e.row.rowIndex
                    let grid = e.component
                    var currentRecord = e.row.data;
                    e.setValue(args.value);

                    if (currentRecord.is_geared != null) {
                        var res = calculateLaytimeAllowed(args.value, currentRecord.loading_rate);
                        grid.cellValue(index, "laytime_duration", res.value);
                        grid.cellValue(index, "laytime_text", res.text);
                    }
                };
            }  

            if (e.parentType === "dataRow" && e.dataField === "loading_rate") {
                e.editorOptions.disabled = !e.row.data.edit_loading_rate;
                e.editorOptions.onValueChanged = args => {
                    let index = e.row.rowIndex
                    let grid = e.component
                    var currentRecord = e.row.data;

                    e.setValue(args.value);

                    if (currentRecord.is_geared != null) {
                        var res = calculateLaytimeAllowed(currentRecord.required_quantity, args.value);
                        grid.cellValue(index, "laytime_duration", res.value);
                        grid.cellValue(index, "laytime_text", res.text);
                    }
                };
            }  

               
        },
        onEditorPrepared: function (e) {
          
            if (e.parentType == "dataRow" && e.dataField == "credit_limit_alert") {
                let inputElement = e.editorElement.find(".dx-texteditor-input-container");
                let color = e.row.data.credit_limit_color

                if (color == "red") {
                    inputElement.addClass('input-alert alert-red');
                }

                if (color == "yellow") {
                    inputElement.addClass('input-alert alert-yellow');
                }

                if (color == "green") {
                    inputElement.addClass('input-alert alert-green');
                }
            }
        },
        onFieldDataChanged: function (e) {
            var updatedField = e.dataField;
            var newValue = e.value;
            console.log("updatedField: ", updatedField);
            console.log("newValue: ", newValue);

            // Event handling commands go here
            //console.log("putra");
            //if (updatedField == "loading_rate") {
            //    let standardHandler = e.editorOptions.onValueChanged

            //    let index = e.row.rowIndex
            //    let grid = e.component

            //    let vesselId = e.value
            //    var currentRecord = e.row.data;
            //    var newValue = e.value;
            //    //console.log(newValue, "newValue");
            //}
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
            pageSize: 20
        },
        pager: {
            allowedPageSizes: [10, 20, 50, 100],
            showNavigationButtons: true,
            showPageSizeSelector: true,
            showInfo: true,
            visible: true
        },
        height: 800,
        showBorders: true,
        editing: {
            mode: "form",
            allowAdding: true,
            allowUpdating: true,
            allowDeleting: true,
            useIcons: true,
            form: {
                colCount: 2,
                items: [
                    {
                        dataField: "despatch_order_number",
                        colSpan: 2
                    },
                    {
                        dataField: "despatch_order_date",
                        
                    },
                    {
                        dataField: "despatch_plan_id",
                       
                    },
                    {
                        dataField: "delivery_term_id",
                    },
                    {
                        dataField: "multiple_barge",
                    },
                    {
                        dataField: "laycan_start",
                    },
                    {
                        dataField: "laycan_end",
                    },
                    {
                        dataField: "laycan_committed",
                        colSpan: 2
                    },
                    {
                        dataField: "eta_plan"
                    },
                    {
                        dataField: "eta_committed"
                    },
                    {
                        dataField: "vessel_id",
                        colSpan: 2
                    },
                    {
                        dataField: "is_geared"
                    },
                    {
                        dataField: "vessel_committed"
                    },
                    //{
                    //    dataField: "bill_of_lading_date",
                    //},
                    {
                        dataField: "sales_contract_id",
                    },
                    {
                        dataField: "contract_term_id",
                    },
                    {
                        dataField: "required_quantity",
                        editorType: "dxNumberBox",
                        editorOptions: {
                            format: "fixedPoint",
                        },
                    },
                    {
                        dataField: "turn_time",
                        editorType: "dxNumberBox",
                        editorOptions: {
                            format: "fixedPoint",
                        },
                    },
                    {
                        dataField: "loading_rate",
                        editorType: "dxNumberBox",
                        editorOptions: {
                            format: "fixedPoint",
                        },
                    },
                    {
                        dataField: "edit_loading_rate"
                    },
                    {
                        dataField: "despatch_demurrage_rate",
                        editorType: "dxNumberBox",
                        editorOptions: {
                            format: "fixedPoint",
                        },
                    },
                    {
                        dataField: "edit_despatch_rate"
                    },
                    {
                        dataField: "despatch_percentage",
                        editorType: "dxNumberBox",
                        editorOptions: {
                            format: "fixedPoint",
                        },
                    },
                    {
                        dataField: "currency_code",
                    },
                    {
                        dataField: "laytime_text",
                        colSpan: 2
                    },
                    {
                        dataField: "seller_id",
                    },
                    {
                        dataField: "customer_id",
                    },
                    {
                        dataField: "ship_to",
                    },
                    {
                        dataField: "uom_id",
                    },
                    {
                        dataField: "contract_product_id",
                    },
                    {
                        dataField: "product_name",
                    },
                    /*{
                        dataField: "final_quantity",
                        editorType: "dxNumberBox",
                        editorOptions: {
                            format: "fixedPoint",
                        },
                    },*/
                    //{
                    //    dataField: "quantity",
                    //    editorType: "dxNumberBox",
                    //    editorOptions: {
                    //        format: "fixedPoint",
                    //    },
                    //},
                    //{
                    //    dataField: "quantity_actual",
                    //    editorType: "dxNumberBox",
                    //    editorOptions: {
                    //        format: "fixedPoint",
                    //    },
                    //},
                    {
                        dataField: "loading_port"
                    },
                    {
                        dataField: "discharge_port"
                    },
                    //{
                    //    dataField: "fulfilment_type_id",
                    //},
                    //{
                    //    dataField: "document_reference_id",
                    //    colSpan: 2
                    //},
                    {
                        dataField: "letter_of_credit",
                        colSpan: 2
                    },
                    {
                        dataField: "surveyor_id",
                    },
                    {
                        dataField: "shipping_agent",
                    },
                    {
                        dataField: "notes",
                        editorType: "dxTextArea",
                        editorOptions: {
                            height: 50,
                            maxLength: 1000,
                            placeholder: "Max 1000 characters",
                        },
                        colSpan: 2
                    },
                    {
                        dataField: "credit_limit_alert"
                    },
                    //{
                    //    dataField: "quantity"
                    //}
                ],
                customizeItem: function (item) {
                    if (item.dataField === 'credit_limit_alert') {
                        item.visible = grid.__creditLimitAlertIsVisible;
                    }
                }
            }
        },
        grouping: {
            contextMenuEnabled: true,
            autoExpandAll: false
        },
        onContentReady: function (e) {
            let grid = e.component
            let queryString = window.location.search
            let params = new URLSearchParams(queryString)

            let despatchOrderId = params.get("Id")

            if (despatchOrderId) {
                grid.filter(["id", "=", despatchOrderId])

                /* Open edit form */
                if (params.get("openEditingForm") == "true") {
                    let rowIndex = grid.getRowIndexByKey(despatchOrderId)

                    grid.editRow(rowIndex)
                }
            }
        },
        onInitNewRow: function (e) {
            e.component.__creditLimitAlertIsVisible = true
        },
        onEditingStart: function (e) {
            console.log("onEditingStart", e)
            e.component.__creditLimitAlertIsVisible = false
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
    }).dxDataGrid("instance");

    // Despatch Order Information Container
    const renderDespatchOrderInformation = (currentRecord, container) => {
        let billOfLadingDateFormatted = currentRecord.bill_of_lading_date ? moment(currentRecord.bill_of_lading_date).format("D MMM YYYY") : '-'
        let draftSurveyBillOfLadingDateFormatted = currentRecord.draft_survey_bill_lading_date ? moment(currentRecord.draft_survey_bill_lading_date).format("D MMM YYYY") : '-'
        let laycanStartFormatted = currentRecord.laycan_start ? moment(currentRecord.laycan_start).format("D MMM YYYY") : '-'
        let laycanEndFormatted = currentRecord.laycan_end ? moment(currentRecord.laycan_end).format("D MMM YYYY") : '-'
        let etaPlanFormatted = currentRecord.laycan_end ? moment(currentRecord.eta_plan).format("D MMM YYYY") : '-'
        let orderRefDateFormatted = currentRecord.despatch_order_date ? moment(currentRecord.despatch_order_date).format("D MMM YYYY") : '-'

        console.log(currentRecord)

        $(`
            <div>
                <div class="row align-items-center mb-3">
                    <div class="col-md-6">
                        <h5>Despatch Order</h5>
                    </div>
                </div>
                
                <div class="row mb-3">
                    <div class="col-md-6">
                        <div class="master-detail-caption mb-2">Overview</div>
                        <div class="card card-mcs card-headline">
                            <div class="row">
                                <div class="col-md-6 pr-0">
                                    <div class="headline-title-container">
                                        <small class="font-weight-normal d-block mb-1">Despatch Order Number</small>
                                        <h4 class="headline-title font-weight-bold">${(currentRecord.despatch_order_number ? currentRecord.despatch_order_number : "-")}</h4>
                                    </div>
                                </div>
                                <div class="col-md-6 pl-0">
                                    <div class="headline-detail-container">
                                        <div class="row">
                                            <div class="col-md-12">
                                                <div class="d-flex align-items-start mb-3">
                                                    <div class="d-inline-block mr-3">
                                                        <div class="icon-circle">
                                                            <i class="fas fa-list fa-sm"></i>
                                                        </div>
                                                    </div>
                                                    <div class="d-inline-block">
                                                        <small class="font-weight-normal text-muted d-block mb-1">Contract Term</small>
                                                        <h5 class="font-weight-bold">${(currentRecord.contract_term_name ? currentRecord.contract_term_name : "-")}</h5>
                                                    </div>
                                                </div>
                                                <div class="d-flex align-items-start mb-3">
                                                    <div class="d-inline-block mr-3">
                                                        <div class="icon-circle">
                                                            <i class="fas fa-building fa-sm"></i>
                                                        </div>
                                                    </div>
                                                    <div class="d-inline-block">
                                                        <small class="font-weight-normal text-muted d-block mb-1">Seller</small>
                                                        <h5 class="font-weight-bold">${(currentRecord.seller_name ? currentRecord.seller_name : "-")}</h5>
                                                    </div>
                                                </div>
                                                <div class="d-flex align-items-start mb-3">
                                                    <div class="d-inline-block mr-3">
                                                        <div class="icon-circle">
                                                            <i class="fas fa-user fa-sm"></i>
                                                        </div>
                                                    </div>
                                                    <div class="d-inline-block">
                                                        <small class="font-weight-normal text-muted d-block mb-1">Buyer</small>
                                                        <h5 class="font-weight-bold">${(currentRecord.customer_name ? currentRecord.customer_name : "-")}</h5>
                                                    </div>
                                                </div>
                                                <div class="d-flex align-items-start">
                                                    <div class="d-inline-block mr-3">
                                                        <div class="icon-circle">
                                                            <i class="fas fa-ship fa-sm"></i>
                                                        </div>
                                                    </div>
                                                    <div class="d-inline-block">
                                                        <small class="font-weight-normal text-muted d-block mb-1">Ship To</small>
                                                        <h5 class="font-weight-bold">${(currentRecord.ship_to_name ? currentRecord.ship_to_name : "-")}</h5>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="col-md-6">
                        <div class="master-detail-caption mb-2">Order Information</div>
                        <div class="card card-mcs card-headline">
                            <div class="row">
                                <div class="col-md-12">
                                    <div class="headline-detail-container">
                                        <div class="row mb-2">
                                            <div class="col-md-12">
                                                <small class="font-weight-normal text-muted d-block mb-1">Despatch Order Number</small>
                                                <p class="font-weight-bold m-0">${(currentRecord.despatch_order_number ? currentRecord.despatch_order_number : "-")}</p>
                                            </div>
                                        </div>
                                        <div class="row mb-2">
                                            <div class="col-md-6">
                                                <small class="font-weight-normal text-muted d-block mb-1">Despatch Order Date</small>
                                                <p class="font-weight-bold m-0">${orderRefDateFormatted}</p>
                                            </div>
                                            <div class="col-md-6">
                                                <small class="font-weight-normal text-muted d-block mb-1">Despatch Plan</small>
                                                <p class="font-weight-bold m-0">${(currentRecord.despatch_plan_name ? currentRecord.despatch_plan_name : "-")}</p>
                                            </div>
                                        </div>
                                        <div class="row mb-2">
                                            <div class="col-md-6">
                                                <small class="font-weight-normal text-muted d-block mb-1">COW</small>
                                                <p class="font-weight-bold m-0">${(currentRecord.draft_survey_number ? currentRecord.draft_survey_number : "-")}</p>
                                            </div>
                                            <div class="col-md-6">
                                                <small class="font-weight-normal text-muted d-block mb-1">COW Quantity</small>
                                                <p class="font-weight-bold m-0">${(currentRecord.draft_survey_quantity ? currentRecord.draft_survey_quantity : "-")}</p>
                                            </div>
                                        </div>
                                        <div class="row mb-2">
                                            <div class="col-md-6">
                                                <small class="font-weight-normal text-muted d-block mb-1">Bill of Lading Date</small>
                                                <p class="font-weight-bold m-0">${draftSurveyBillOfLadingDateFormatted}</p>
                                            </div>
                                            <div class="col-md-6">
                                                <small class="font-weight-normal text-muted d-block mb-1">Bill of Lading Number</small>
                                                <p class="font-weight-bold m-0">${(currentRecord.draft_survey_bill_lading_number ? currentRecord.draft_survey_bill_lading_number : "-")}</p>
                                            </div>
                                        </div>
                                        <div class="row mb-2">
                                            <div class="col-md-6">
                                                <small class="font-weight-normal text-muted d-block mb-1">COA</small>
                                                <p class="font-weight-bold m-0">${(currentRecord.quality_sampling_number ? currentRecord.quality_sampling_number : "-")}</p>
                                            </div>
                                        </div>
                                        <div class="row mb-2">
                                            <div class="col-md-6">
                                                <small class="font-weight-normal text-muted d-block mb-1">Despatch Fulfilment Type</small>
                                                <p class="font-weight-bold m-0">${(currentRecord.fulfilment_type_name ? currentRecord.fulfilment_type_name : "-")}</p>
                                            </div>
                                            <div class="col-md-6">
                                                <small class="font-weight-normal text-muted d-block mb-1">Contract Product</small>
                                                <p class="font-weight-bold m-0">${(currentRecord.sales_contract_product_name ? currentRecord.sales_contract_product_name : "-")}</p>
                                            </div>
                                        </div>
                                        <div class="row mb-2">
                                            <div class="col-md-6">
                                                <small class="font-weight-normal text-muted d-block mb-1">Delivery Term</small>
                                                <p class="font-weight-bold m-0">${(currentRecord.delivery_term_name ? currentRecord.delivery_term_name : "-")}</p>
                                            </div>
                                            <div class="col-md-6">
                                                <small class="font-weight-normal text-muted d-block mb-1">Unit</small>
                                                <p class="font-weight-bold m-0">${(currentRecord.uom_name ? currentRecord.uom_name : "-")}</p>
                                            </div>
                                        </div>
                                        <div class="row mb-2">
                                            <div class="col-md-6">
                                                <small class="font-weight-normal text-muted d-block mb-1">Letter of Credit</small>
                                                <p class="font-weight-bold m-0">${(currentRecord.letter_of_credit ? currentRecord.letter_of_credit : "-")}</p>
                                            </div>
                                            <div class="col-md-6">
                                                <small class="font-weight-normal text-muted d-block mb-1">Notes</small>
                                                <p class="font-weight-bold m-0">${(currentRecord.notes ? currentRecord.notes : "-")}</p>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="row mb-5">
                    <div class="col-md-6">
                        <div class="master-detail-caption mb-2">Shipment Information</div>
                        <div class="card card-mcs card-headline">
                            <div class="row">
                                <div class="col-md-12">
                                    <div class="headline-detail-container">
                                        <div class="row mb-2">
                                            <div class="col-md-6">
                                                <small class="font-weight-normal text-muted d-block mb-1">Laycan Start</small>
                                                <p class="font-weight-bold m-0">${laycanStartFormatted}</p>
                                            </div>
                                            <div class="col-md-6">
                                                <small class="font-weight-normal text-muted d-block mb-1">Laycan End</small>
                                                <p class="font-weight-bold m-0">${laycanEndFormatted}</p>
                                            </div>
                                        </div>
                                        <div class="row mb-2">
                                            <div class="col-md-6">
                                                <small class="font-weight-normal text-muted d-block mb-1">ETA Plan</small>
                                                <p class="font-weight-bold m-0">${(etaPlanFormatted)}</p>
                                            </div>
                                            <div class="col-md-6">
                                                <small class="font-weight-normal text-muted d-block mb-1">Bill of Lading Date</small>
                                                <p class="font-weight-bold m-0">${billOfLadingDateFormatted}</p>
                                            </div>
                                        </div>
                                        <div class="row mb-2">
                                            <div class="col-md-6">
                                                <small class="font-weight-normal text-muted d-block mb-1">Laycan Commmitted</small>
                                                <p class="font-weight-bold m-0">${(currentRecord.laycan_committed == true ? "Yes" : "No")}</p>
                                            </div>
                                            <div class="col-md-6">
                                                <small class="font-weight-normal text-muted d-block mb-1">ETA Committed</small>
                                                <p class="font-weight-bold m-0">${(currentRecord.eta_committed == true ? "Yes" : "No")}</p>
                                            </div>
                                        </div>
                                        <div class="row mb-2">
                                            <div class="col-md-6">
                                                <small class="font-weight-normal text-muted d-block mb-1">Vessel</small>
                                                <p class="font-weight-bold m-0">${(currentRecord.vehicle_name ? currentRecord.vehicle_name : "-")}</p>
                                            </div>
                                            <div class="col-md-6">
                                                <small class="font-weight-normal text-muted d-block mb-1">Vessel Committed</small>
                                                <p class="font-weight-bold m-0">${(currentRecord.vessel_committed == true ? "Yes" : "No")}</p>
                                            </div>
                                        </div>
                                        <div class="row mb-2">
                                            <div class="col-md-6">
                                                <small class="font-weight-normal text-muted d-block mb-1">Loading Port</small>
                                                <p class="font-weight-bold m-0">${(currentRecord.loading_port ? currentRecord.loading_port : "-")}</p>
                                            </div>
                                            <div class="col-md-6">
                                                <small class="font-weight-normal text-muted d-block mb-1">Discharge Port</small>
                                                <p class="font-weight-bold m-0">${(currentRecord.discharge_port == true ? currentRecord.discharge_port : "-")}</p>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `).appendTo(container)
    }

    // Despatch Order Product Specification 
    const renderDespatchOrderProductSpecification = (currentRecord, container) => {
        var urlDetail = "/api/StockpileManagement/QualitySamplingAnalyte";

        $("<div>")
            .addClass("master-detail-caption")
            .text("COA Result")
            .appendTo(container);

        $("<div>")
            .dxDataGrid({
                dataSource: DevExpress.data.AspNet.createStore({
                    key: "id",
                    loadUrl: urlDetail + "/ByDespatchOrderId/" + encodeURIComponent(currentRecord.id),
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
                        dataField: "analyte_id",
                        dataType: "text",
                        caption: "Analyte",
                        validationRules: [{
                            type: "required",
                            message: "The field is required."
                        }],
                        lookup: {
                            dataSource: DevExpress.data.AspNet.createStore({
                                key: "value",
                                loadUrl: urlDetail + "/AnalyteIdLookup",
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
                        allowEditing: false
                    },
                    {
                        dataField: "target",
                        dataType: "number",
                        caption: "Target",
                        allowEditing: false
                    },
                    {
                        dataField: "minimum",
                        dataType: "number",
                        caption: "Minimum",
                        allowEditing: false
                    },
                    {
                        dataField: "maximum",
                        dataType: "number",
                        caption: "Maximum",
                        allowEditing: false
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
                    useIcons: true,
                    form: {
                        colCount: 2,
                        items: [
                            {
                                dataField: "analyte_id",
                            },
                            {
                                dataField: "uom_id",
                            },
                            {
                                dataField: "analyte_value"
                            },
                        ],
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

    const renderTranshipment = (currentRecord, container) => {
        var urlDetail = "/api/Sales/SalesInvoiceTranshipment";

        $("<div>")
            .addClass("master-detail-caption")
            .text("Transhipment")
            .appendTo(container);

        $("<div>")
            .dxDataGrid({
                dataSource: DevExpress.data.AspNet.createStore({
                    key: "id",
                    loadUrl: urlDetail + "/ByDespatchOrderId/" + encodeURIComponent(currentRecord.id),
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
                        sortOrder: "asc",
                        allowEditing: false
                    },
                    {
                        dataField: "start_datetime",
                        dataType: "datetime",
                        sortOrder: "asc",
                        caption: "Commenced Loading DateTime",
                        allowEditing: false
                    },
                    {
                        dataField: "end_datetime",
                        dataType: "datetime",
                        sortOrder: "asc",
                        caption: "Completed Loading DateTime",
                        allowEditing: false
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
                    useIcons: true,
                    form: {
                        colCount: 2,
                        items: [
                            {
                                dataField: "transaction_number",
                            },
                            {
                                dataField: "start_datetime",
                            },
                            {
                                dataField: "end_datetime",
                            },
                           
                        ],
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
                    e.data.start_datetime = currentRecord.start_datetime;
                    e.data.end_datetime = currentRecord.end_datetime;
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


    // Despatch Order Delay Definition
    const renderDespatchOrderDelayDefinition = (currentRecord, container) => {
        var detailName = "DespatchOrderDelay";
        var urlDetail = "/api/" + areaName + "/" + detailName;

        $("<div>")
            .addClass("master-detail-caption")
            .text("Delay Definitions")
            .appendTo(container);

        $("<div>")
            .dxDataGrid({
                dataSource: DevExpress.data.AspNet.createStore({
                    key: "id",
                    loadUrl: urlDetail + "/ByDespatchOrderId/" + encodeURIComponent(currentRecord.id),
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
                        dataField: "despatch_order_id",
                        allowEditing: false,
                        visible: false,
                        calculateCellValue: function () {
                            return currentRecord.id;
                        }
                    },
                    {
                        dataField: "analyte_id",
                        dataType: "string",
                        caption: "Analyte Definition",
                        allowEditing: false,
                        lookup: {
                            dataSource: DevExpress.data.AspNet.createStore({
                                key: "value",
                                loadUrl: "/api/Quality/Analyte/AnalyteIdLookup",
                                onBeforeSend: function (method, ajaxOptions) {
                                    ajaxOptions.xhrFields = { withCredentials: true };
                                    ajaxOptions.beforeSend = function (request) {
                                        request.setRequestHeader("Authorization", "Bearer " + token);
                                    };
                                }
                            }),
                            searchEnabled: true,
                            valueExpr: "value",
                            displayExpr: "text"
                        },
                    },
                    {
                        dataField: "analyte_standard_id",
                        dataType: "string",
                        caption: "Standard",
                        allowEditing: false,
                        lookup: {
                            dataSource: function (options) {
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
                                    filter: ["item_group", "=", "analyte-standard"]
                                }
                            },
                            valueExpr: "value",
                            displayExpr: "text"
                        },
                    },
                    {
                        dataField: "value",
                        dataType: "number",
                        caption: "Value",
                        allowEditing: false
                    },
                    {
                        dataField: "target",
                        dataType: "number",
                        caption: "Target",
                        allowEditing: false
                    }
                    /*{
                        dataField: "demurrage_percent",
                        dataType: "number",
                        caption: "Demurrage %",
                        customizeText: function (cellInfo) {
                            return numeral(cellInfo.value).format('0,0.00');
                        }
                    },
                    {
                        dataField: "despatch_percent",
                        dataType: "number",
                        caption: "Despatch %",
                        customizeText: function (cellInfo) {
                            return numeral(cellInfo.value).format('0,0.00');
                        }
                    }*/
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
                    useIcons: true,
                    form: {
                        colCount: 2,
                        items: [
                            {
                                dataField: "analyte_id"
                            },
                            {
                                dataField: "analyte_standard_id"
                            },
                            {
                                dataField: "value"
                            },
                            {
                                dataField: "target"
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
                    e.data.despatch_order_id = currentRecord.id;
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

    // Initial Information Popup
    let initialInformationPopupOptions = {
        title: "Initial Information",
        height: "'auto'",
        closeOnOutsideClick: true,
        contentTemplate: function () {

            var initialInformationForm = $("<div>").dxForm({
                formData: {
                    despatch_order_id: "",
                    vessel_name: "",
                    eta_plan: "",
                    loading_port: "",
                    seller: "",
                    customer_name: "",
                    customer_address: "",
                    customer_additional_info: "",
                    contract_product_name: "",
                    status: "",
                },
                colCount: 2,
                readOnly: true,
                items: [
                    {
                        dataField: "despatch_order_id",
                        label: {
                            text: "Despatch Order Id"
                        },
                        visible: false
                    },
                    {
                        dataField: "seller",
                        label: {
                            text: "Seller Name",
                        },
                    },
                    {
                        dataField: "customer_name",
                        label: {
                            text: "Customer Name",
                        },
                    },
                    {
                        dataField: "customer_address",
                        label: {
                            text: "Customer Primary Address",
                        },
                        editorType: "dxTextArea",
                    },
                    {
                        dataField: "customer_additional_info",
                        label: {
                            text: "Customer Additional Information",
                        },
                        editorType: "dxTextArea",
                    },
                    {
                        dataField: "contract_product_name",
                        label: {
                            text: "Contract Product Name"
                        },
                    },
                    {
                        dataField: "eta_plan",
                        label: {
                            text: "ETA Plan"
                        },
                        editorType: "dxDateBox",
                    },
                    {
                        dataField: "vessel_name",
                        label: {
                            text: "Vessel Name"
                        },
                    },
                    {
                        dataField: "loading_port",
                        label: {
                            text: "Loading Port"
                        },
                    },

                    {
                        dataField: "status",
                        label: {
                            text: "Status"
                        },
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
                                let data = initialInformationForm.dxForm("instance").option("formData");
                                let formData = new FormData()
                                formData.append("key", data.despatch_order_id)
                                formData.append("values", JSON.stringify(data))

                                saveInitialInformationForm(formData)
                            }
                        }
                    }
                ],
                onInitialized: () => {
                    $.ajax({
                        type: "GET",
                        url: "/api/Sales/InitialInformation/GetByDespatchOrderId/" + encodeURIComponent(despatchOrderData.id),
                        contentType: "application/json",
                        beforeSend: function (xhr) {
                            xhr.setRequestHeader("Authorization", "Bearer " + token);
                        },
                        success: function (response) {
                            // Update form formData with response from api
                            if (response) {
                                initialInformationForm.dxForm("instance").option("formData", response)
                            }
                        }
                    })
                }
            })

            return initialInformationForm;
        }
    }
    var initialInformationPopup = $("#initial-information-popup").dxPopup(initialInformationPopupOptions).dxPopup("instance")

    const showInitialInformationPopup = function () {
        initialInformationPopup.option("contentTemplate", initialInformationPopupOptions.contentTemplate.bind(this));
        initialInformationPopup.show()
    }

    const saveInitialInformationForm = (formData) => {
        $.ajax({
            type: "POST",
            url: "/api/Sales/InitialInformation/InsertData",
            data: formData,
            processData: false,
            contentType: false,
            beforeSend: function (xhr) {
                xhr.setRequestHeader("Authorization", "Bearer " + token);
            },
            success: function (response) {
                if (response) {
                    // Show successfuly saved popup
                    let successPopup = $("<div>").dxPopup({
                        width: 300,
                        height: "auto",
                        dragEnabled: false,
                        closeOnOutsideClick: true,
                        showTitle: true,
                        title: "Success",
                        contentTemplate: function () {
                            return $(`<p class="text-center">Initial Information saved.</p>`)
                        }
                    }).appendTo("body").dxPopup("instance")
                    successPopup.show()
                }
            }
        }).fail(function (jqXHR, textStatus, errorThrown) {
            initialInformationPopup.hide();
            Swal.fire("Failed !", jqXHR.responseText, "error");
        });
    }

    var calculateLaytimeAllowed = function (cargo_quantity, loading_rate) {
        console.log("cargo_quantity", cargo_quantity);
        console.log("loading_rate", loading_rate);
        var result = {
            value: 0,
            text: ""
        }
        if (cargo_quantity === undefined || loading_rate === undefined || cargo_quantity === 0 || loading_rate === 0) {
            return result
        } 
        result.value = parseInt((parseFloat(cargo_quantity) / parseFloat(loading_rate)) * 86400);
        result.text = secondsToDhms(result.value);
        return result;
    }

    function secondsToDhms(seconds) {
        seconds = Number(seconds);
        var d = Math.floor(seconds / (3600 * 24));
        var h = Math.floor(seconds % (3600 * 24) / 3600);
        var m = Math.floor(seconds % 3600 / 60);
        var s = Math.floor(seconds % 60);

        var dDisplay = d > 0 ? d + (d == 1 ? " Day " : " Days ") : "";
        var hDisplay = h > 0 ? h + (h == 1 ? " Hour " : " Hours ") : "";
        var mDisplay = m > 0 ? m + (m == 1 ? " Minute " : " Minutes ") : "";
        return dDisplay + hDisplay + mDisplay;
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
                url: "/api/Sales/DespatchOrder/UploadDocument",
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

    /**
     * =========================
     * Attachments Grid
     * =========================
     */

    const maxFileSize = 52428800;

    let despatchOrderDocumentDataGrid
    const renderDespatchOrderDocument = function (currentRecord, container) {
        let attachmentUrlDetail = "/api/Sales/DespatchOrderDocument";
        let despatchOrderDocumentContainer = $("<div class='mb-5'>")
        despatchOrderDocumentContainer.appendTo(container)

        let titleContainer = $(`
            <div class="row mb-3 mt-6 align-items-center">
                <div class="col-md-6">
                    <div class="master-detail-caption">File Attachments</div>
                </div>
                <div class="col-md-6 btn-container float-right">
                </div>
            </div>
        `).appendTo(despatchOrderDocumentContainer)

        $("<div>")
            .dxButton({
                stylingMode: "contained",
                icon: "plus",
                text: "Add Attachments",
                type: "normal",
                width: "'auto'",
                onClick: function () {
                    openAddAttachmentPopup()
                },
                elementAttr: {
                    class: "float-right"
                }
            }).appendTo(titleContainer.find(".btn-container")[0])

        despatchOrderDocumentDataGrid = $("<div>")
            .dxDataGrid({
                dataSource: DevExpress.data.AspNet.createStore({
                    key: "id",
                    //loadUrl: attachmentUrlDetail + "/DataGrid?Id=" + encodeURIComponent(currentRecord.id),
                    loadUrl: attachmentUrlDetail + "/ByDespatchOrderId/" + encodeURIComponent(currentRecord.id),
                    insertUrl: attachmentUrlDetail + "/InsertData",
                    updateUrl: attachmentUrlDetail + "/UpdateData",
                    deleteUrl: attachmentUrlDetail + "/DeleteData",
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
                        dataField: "despatch_order_id",
                        dataType: "string",
                        allowEditing: false,
                        visible: false,
                        calculateCellValue: function () {
                            return currentRecord.id;
                        }
                    },
                    {
                        dataField: "filename",
                        dataType: "string",
                        caption: "File name",
                        formItem: {
                            colSpan: 2
                        },
                        cellTemplate: function (container, options) {
                            let attachmentUrl = options.value
                            let attachmentName = /[^\\]*$/.exec(attachmentUrl)[0] // Get only the file name and its extension

                            $(`<span><i class="fas fa-file mr-2"></i>${attachmentName}</span>`).appendTo(container)
                        }

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
                                let attachment = e.row.data
                                let attachmentName = /[^\\]*$/.exec(attachment.filename)[0]

                                let xhr = new XMLHttpRequest()
                                xhr.open("GET", "/api/Sales/DespatchOrderDocument/DownloadDocument/" + attachment.id, true)
                                xhr.responseType = "blob"
                                xhr.setRequestHeader("Authorization", "Bearer " + token)

                                xhr.onload = function (e) {
                                    let blobURL = window.webkitURL.createObjectURL(xhr.response)

                                    let a = document.createElement("a")
                                    a.href = blobURL
                                    a.download = attachmentName
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
                    e.data.business_partner_id = currentRecord.id;
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
            }).appendTo(despatchOrderDocumentContainer).dxDataGrid("instance");
    }

    const addAttachmentPopupOptions = {
        width: 500,
        height: "auto",
        showTitle: true,
        title: "Add Attachment",
        visible: false,
        dragEnabled: false,
        closeOnOutsideClick: true,
        contentTemplate: function () {
            let despatchOrderIdInput =
                $("<div>")
                    .dxTextBox({
                        name: "despatch_order_id",
                        value: despatchOrderData.id,
                        readOnly: true,
                        visible: false
                    })

            let attachmentInput =
                $("<div class='mb-5 dx-fileuploader-mcs'>")
                    .dxFileUploader({
                        uploadMode: "useForm",
                        multiple: false
                    })

            let submitButton =
                $("<div>")
                    .dxButton({
                        text: "Submit",
                        onClick: function (e) {
                            let despatchOrderId = despatchOrderIdInput.dxTextBox("instance").option("value")
                            let file = attachmentInput.dxFileUploader("instance").option("value")[0]

                            var reader = new FileReader();
                            reader.readAsDataURL(file);
                            reader.onload = function () {
                                let fileName = file.name
                                let fileSize = file.size
                                let data = reader.result.split(',')[1]

                                if (fileSize == 0) {
                                    alert("File content is empty.")
                                    return;
                                }
                                if (fileSize >= maxFileSize) {
                                    alert("File size exceeds 50 MB.");
                                    return;
                                }

                                let formData = {
                                    "despatchOrderId": despatchOrderId,
                                    "fileName": fileName,
                                    "fileSize": fileSize,
                                    "data": data
                                }

                                $.ajax({
                                    url: "/api/Sales/DespatchOrderDocument/InsertData",
                                    data: JSON.stringify(formData),
                                    type: "POST",
                                    contentType: "application/json",
                                    beforeSend: function (xhr) {
                                        xhr.setRequestHeader("Authorization", "Bearer " + token);
                                    },
                                    success: function (response) {
                                        addAttachmentPopup.hide()
                                        despatchOrderDocumentDataGrid.refresh()
                                    }
                                })
                            }
                        }
                    })

            let formContainer = $("<form enctype='multipart/form-data'>")
                .append(despatchOrderIdInput, attachmentInput, submitButton)

            return formContainer;
        }
    }

    const addAttachmentPopup = $("<div>")
        .dxPopup(addAttachmentPopupOptions).appendTo("body").dxPopup("instance")

    const openAddAttachmentPopup = function () {
        addAttachmentPopup.option("contentTemplate", addAttachmentPopupOptions.contentTemplate.bind(this));
        addAttachmentPopup.show()
    }

});