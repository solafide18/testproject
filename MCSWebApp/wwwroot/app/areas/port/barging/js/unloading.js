$(function () {

    var token = $.cookie("Token");
    var areaName = "Port";
    var entityName = "Barging";
    var url = "/api/" + areaName + "/" + entityName;
    const maxFileSize = 52428800;

    var skipValidationCheck = false;

    var bargingTransactionData;


    function formatTanggal(x) {
        theDate = new Date(x);
        formatted_date = theDate.getFullYear() + "-" + (theDate.getMonth() + 1).toString().padStart(2, "0") + "-"
            + theDate.getDate().toString().padStart(2, "0") + " " + theDate.getHours().toString().padStart(2, "0")
            + ":" + theDate.getMinutes().toString().padStart(2, "0");
        return formatted_date;
    }

    var tgl1 = sessionStorage.getItem("BargeUnloadingDate1");
    var tgl2 = sessionStorage.getItem("BargeUnloadingDate2");

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
            sessionStorage.setItem("BargeUnloadingDate1", formatTanggal(firstDay));
            _loadUrl = url + "/Unloading/DataGrid/" + encodeURIComponent(formatTanggal(firstDay))
                + "/" + encodeURIComponent(formatTanggal(lastDay));
        }
    });

    $("#date-box2").dxDateBox({
        type: "datetime",
        displayFormat: 'dd MMM yyyy HH:mm',
        value: lastDay,
        onValueChanged: function (data) {
            lastDay = new Date(data.value);
            sessionStorage.setItem("BargeUnloadingDate2", formatTanggal(lastDay));
            _loadUrl = url + "/Unloading/DataGrid/" + encodeURIComponent(formatTanggal(firstDay))
                + "/" + encodeURIComponent(formatTanggal(lastDay));
        }
    });

    $('#btnView').on('click', function () {
        location.reload();
    })

    var _loadUrl = url + "/Unloading/DataGrid/" + encodeURIComponent(formatTanggal(firstDay))
        + "/" + encodeURIComponent(formatTanggal(lastDay));

    $("#grid").dxDataGrid({
        dataSource: DevExpress.data.AspNet.createStore({
            key: "id",
            //loadUrl: url + "/Unloading/DataGrid",
            loadUrl: _loadUrl,
            insertUrl: url + "/Unloading/InsertData",
            updateUrl: url + "/Unloading/UpdateData",
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
                dataField: "reference_number",
                dataType: "string",
                caption: "Draft Survey Number",
                visible: false
            },
            {
                dataField: "despatch_order_id",
                dataType: "text",
                caption: "Despatch Order",
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
                dataField: "delivery_term_name",
                caption: "Delivery Term",
                dataType: "string",
                visible: false,
                allowEditing: false,
            },
            {
                dataField: "process_flow_id",
                dataType: "text",
                caption: "Process Flow",
                visible: false,
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
                    //rowData.source_location_id = null;
                    //rowData.destination_location_id = null;
                },
                calculateSortValue: function (data) {
                    var value = this.calculateCellValue(data);
                    return this.lookup.calculateCellValue(value);
                },
            },
            {
                dataField: "source_location_id",
                dataType: "text",
                caption: "Barge",
                validationRules: [{
                    type: "required",
                    message: "This field is required."
                }],
                /*lookup: {
                    dataSource: function (options) {
                        var _url = url + "/Unloading/SourceLocationIdLookup";

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
            },*/
                lookup: {
                    dataSource: function (options) {
                        var _url = url + "/unloading/SourceLocationIdLookup";

                        if (options !== undefined && options !== null) {
                            if (options.data !== undefined && options.data !== null) {
                                if (options.data.despatch_order_id !== undefined
                                    && options.data.despatch_order_id !== null) {
                                    _url += "?ProcessFlowId=" + encodeURIComponent(options.data.process_flow_id);
                                    //_url += "?DespatchOrderId=" + encodeURIComponent(options.data.despatch_order_id);
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
                    rowData.source_location_id = value;
                },
                calculateSortValue: function (data) {
                    var value = this.calculateCellValue(data);
                    return this.lookup.calculateCellValue(value);
                },
            },

            {
                dataField: "owner_name1",
                dataType: "string",
                caption: "Owner",
                visible: false,
                allowEditing: false
            },
            //{
            //    dataField: "tug_name1",
            //    dataType: "string",
            //    caption: "T U G",
            //    visible: false,
            //    allowEditing: false
            //},
            {
                dataField: "tug_id",
                dataType: "text",
                caption: "T u g",
                visible: false,
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: "/api/Transport/Tug/TugIdLookup",
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
                dataField: "destination_location_id",
                dataType: "text",
                caption: "Destination Location",
                validationRules: [{
                    type: "required",
                    message: "This field is required."
                }],
                lookup: {
                    dataSource: function (options) {
                        var _url = url + "/Unloading/DestinationLocationIdLookup";

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
                    rowData.destination_location_id = value;
                },
                calculateSortValue: function (data) {
                    var value = this.calculateCellValue(data);
                    return this.lookup.calculateCellValue(value);
                },
            },
            {
                dataField: "destination_shift_id",
                dataType: "text",
                caption: "Shift",
                visible: false,
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: url + "/DestinationShiftIdLookup",
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
                dataField: "quantity",
                dataType: "number",
                caption: "Draft Survey Quantity",
                format: "fixedPoint",
                formItem: {
                    editorType: "dxNumberBox",
                    editorOptions: {
                        format: "fixedPoint",
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
                width: "90px",
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
            //{
            //    dataField: "product_id",
            //    dataType: "text",
            //    caption: "Product",
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
            //    setCellValue: function (rowData, value) {
            //        rowData.product_id = value;
            //    },
            //    calculateSortValue: function (data) {
            //        var value = this.calculateCellValue(data);
            //        return this.lookup.calculateCellValue(value);
            //    },
            //},
            {
                dataField: "product_name",
                dataType: "string",
                caption: "Product Name",
                visible: false,
                allowEditing: false
            },
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
                },
            },
            {
                dataField: "hour_usage",
                dataType: "number",
                caption: "Hour Usage",
                visible: false
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
                dataField: "draft_survey_id",
                dataType: "text",
                caption: "Draft Survey",
                visible: false,
                formItem: {
                    colSpan: 2
                },
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
                caption: "Ref. Work Order",
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
                caption: "Commenced Unloading DateTime",
                format: "yyyy-MM-dd HH:mm:ss",
                validationRules: [{
                    type: "required",
                    message: "This field is required."
                }],
            },
            {
                dataField: "end_datetime",
                dataType: "datetime",
                caption: "Completed Unloading DateTime",
                format: "yyyy-MM-dd HH:mm:ss",
                validationRules: [{
                    type: "required",
                    message: "This field is required."
                }]
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
        onRowValidating: function (e) {
            //e.isValid = false;

            

            if (e.isValid) {
                if (skipValidationCheck) {
                    skipValidationCheck = false;
                    return;
                }

                if (!skipValidationCheck && (e.newData.source_location_id != undefined || e.newData.source_location_id != undefined || e.newData.quantity != undefined)) {
                 
                    if (e.oldData === undefined) {
                        unloadingDespatchOrderId = e.newData.despatch_order_id;
                        unloadingSourceLocationId = e.newData.source_location_id;
                        unloadingQuantity = e.newData.quantity;
                        console.log("testing", e.newData);
                    } else {
                        
                        unloadingDespatchOrderId = e.newData.despatch_order_id == undefined ? e.oldData.despatch_order_id : e.newData.despatch_order_id;
                        unloadingSourceLocationId = e.newData.source_location_id == undefined ? e.oldData.source_location_id : e.newData.source_location_id;
                        unloadingQuantity = e.newData.quantity;
                    }

                    e.isValid = false;
                    $.ajax({
                        url: url + '/GetBargingTransactionUnloading/' + unloadingDespatchOrderId + '/' + unloadingSourceLocationId + '',
                        type: 'GET',
                        headers: {
                            "Authorization": "Bearer " + token
                        }
                    }).done(function (result) {
                        console.log("GetBargingTransactionUnloading", result);
                        if (result.status.success) {
                            if (result.data == null) {
                                Swal.fire("Error !", 'Cannot find Barge Loading Data.', "error");
                            } else {
                                
                                if (unloadingQuantity != result.data.quantity) {
                                    //Swal.fire({
                                    //    title: "Loading Quantity and Unloading Quantity is not equal!",
                                    //    text: "Are you sure want so save?",
                                    //    type: "warning",
                                    //    showCancelButton: true,
                                    //    confirmButtonText: "Yes"
                                    //}).then(function (swalResult) {
                                    //    console.log(swalResult);
                                    //    if (swalResult.value) {
                                    //        e.isValid = true;
                                    //        skipValidationCheck = true;
                                    //        $("#grid").dxDataGrid("instance").saveEditData();
                                    //    }
                                    //});
                                    var text = "Loading Quantity and Unloading Quantity is not equal!\nDo you want to Save ?";
                                    if (confirm(text) == true) {
                                        e.isValid = true;
                                        skipValidationCheck = true;
                                        $("#grid").dxDataGrid("instance").saveEditData();
                                    } else {
                                        text = "You canceled!";
                                    }
                                }
                                else {
                                    e.isValid = true;
                                    skipValidationCheck = true;
                                    $("#grid").dxDataGrid("instance").saveEditData();
                                }
                            }
                        }
                    }).fail(function (jqXHR, textStatus, errorThrown) {
                        Swal.fire("Failed !", textStatus, "error");
                    });
                }
            }
            console.log("onRowValidating", e);

            //-- BargeId = Source Location Id in field BargeTransaction
            console.log(e.newData.source_location_id);
            

            //$.ajax({
            //    url: url + '/GetBargingTransactionLoadingByDespatchOrderId/' + encodeURIComponent(e.new),
            //    type: 'GET',
            //    headers: {
            //        "Authorization": "Bearer " + token
            //    }
            //}).done(function (result) {

            //    //Swal.fire({
            //    //    title: "Loading Quantity and Unloading Quantity is not equal!",
            //    //    text: "Are you sure want so save?",
            //    //    type: "warning",
            //    //    showCancelButton: true,
            //    //    confirmButtonText: "Yes"
            //    //}).then(function (result) {
            //    //    if (result.value) {
            //    //        alert();
            //    //    }
            //    //});

            //}).fail(function (jqXHR, textStatus, errorThrown) {
            //    Swal.fire("Failed !", textStatus, "error");
            //});

            
            //if (!e.brokenRules.length)
            //    return false

            //// Ref: https://supportcenter.devexpress.com/ticket/details/t512157/how-to-automatically-focus-the-first-non-validated-field
            //e.brokenRules[0].validator.focus();
        },
        editing: {
            mode: "popup",
            allowAdding: true,
            allowUpdating: true,

            allowDeleting: true,
            useIcons: true,
            form: {
                itemType: "group",
                items: [
                    {
                        dataField: "transaction_number",
                    },
                    {
                        dataField: "process_flow_id",
                    },
                    {
                        dataField: "ref_work_order",
                    },
                   /* {
                        dataField: "accounting_period_id",
                    },*/
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
                        dataField: "source_location_id",
                    },
                    {
                        dataField: "owner_name1",
                    },
                    {
                        dataField: "tug_id",
                    },
                    {
                        dataField: "destination_location_id",
                    },
                    /*{
                        dataField: "destination_shift_id",
                    },*/
                    {
                        dataField: "reference_number",
                    },
                    {
                        dataField: "quantity",
                    },
                    {
                        dataField: "initial_draft_survey",
                    },
                    {
                        dataField: "final_draft_survey",
                    },
                    
                    {
                        dataField: "uom_id",
                    },
                    {
                        dataField: "product_name",
                    },
                    {
                        dataField: "equipment_id",
                    },
                    //{
                    //    dataField: "draft_survey_id",
                    //},
                    {
                        dataField: "hour_usage",
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
                            let record = response.data;
                            // Set its corresponded field's value
                            grid.cellValue(index, "delivery_term_name", record.delivery_term_name);
                            grid.cellValue(index, "product_name", record.product_name);
                            grid.cellValue(index, "owner_name1", record.owner_name)
                            //grid.cellValue(index, "source_location_id", record.vessel_id);
                            if (record.barge_name != null) {
                                grid.cellValue(index, "source_location_id", record.vessel_id);
                                grid.cellValue(index, "tug_id", record.tug_id);
                            }
                        }
                    })
                    standardHandler(e) // Calling the standard handler to save the edited value
                }
            }

            if (e.dataField === "source_location_id" && e.parentType == "dataRow") {
                let standardHandler = e.editorOptions.onValueChanged
                let index = e.row.rowIndex
                let grid = e.component
                let rowData = e.row.data

                e.editorOptions.onValueChanged = function (e) { // Overiding the standard handler

                    // Get its value (Id) on value changed
                    let sourceLocationId = e.value


                    // Get another data from API after getting the Id
                    $.ajax({
                        url: '/api/Transport/Barge/DataBarge?Id=' + sourceLocationId,
                        type: 'GET',
                        contentType: "application/json",
                        beforeSend: function (xhr) {
                            xhr.setRequestHeader("Authorization", "Bearer " + token);
                        },
                        success: function (response) {
                            /*let record = response.data[0]*/
                            let record = response.data;
                            grid.cellValue(index, "owner_name1", record.business_partner_name)
                            grid.cellValue(index, "tug_id", record.tug_id)
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

    let documentDataGrid
    function createDocumentsTabTemplate(masterDetailData) {
        return function () {
            let currentRecord = masterDetailData;
            let detailName = "BargingLoadUnloadDocument";
            let urlDetail = "/api/" + areaName + "/" + detailName;
            bargingTransactionData = currentRecord

            documentDataGrid = $("<div>")
                .dxDataGrid({
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "id",
                        loadUrl: urlDetail + "/ByBargingTransactionId/" + encodeURIComponent(currentRecord.id),
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
                                    xhr.open("GET", "/api/Port/BargingLoadUnloadDocument/DownloadDocument/" + documentData.id, true)
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

    function createQualitySamplingTab(masterDetailData) {
        return function () {
            let currentRecord = masterDetailData;
            let detailName = "Barging";
            let urlDetail = "/api/" + areaName + "/" + detailName;
            bargingTransactionData = currentRecord

            documentDataGrid = $("<div>")
                .dxDataGrid({
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "id",
                        loadUrl: urlDetail + "/ByBargingTransactionId/" + encodeURIComponent(currentRecord.id),
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
                    barging_transaction_id: bargingTransactionData.id,
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
                        dataField: "barging_transaction_id",
                        label: {
                            text: "Barging Transaction Id"
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
                                        "bargingTransId": formData.barging_transaction_id,
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
                                        url: `/api/${areaName}/BargingLoadUnloadDocument/InsertData`,
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
                url: "/api/Port/Barging/Unloading/UploadDocument",
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