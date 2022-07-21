$(function () {

    var token = $.cookie("Token");
    var areaName = "Planning";
    var entityName = "SalesPlan";
    var url = "/api/" + areaName + "/" + entityName;
    var salesPlanDetailData = null
    var CustomerId = ""

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
            //{
            //    dataField: "plan_name",
            //    dataType: "string",
            //    caption: "Sales Plan Name",
            //    validationRules: [{
            //        type: "required",
            //        message: "The field is required."
            //    }],
            //    formItem: {
            //        colSpan: 2
            //    },
            //    sortOrder: "asc"
            //},
            {
                dataField: "plan_year_id",
                dataType: "string",
                caption: "Year",
                validationRules: [{
                    type: "required",
                    message: "The field is required."
                }],
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
                            filter: ["item_group", "=", "years"]
                        }
                    },
                    valueExpr: "value",
                    displayExpr: "text"
                },
                sortOrder: "asc"
            },
            {
                dataField: "site_id",
                dataType: "string",
                caption: "Site",
                validationRules: [{
                    type: "required",
                    message: "The field is required."
                }],
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
                            filter: ["item_group", "=", "site"]
                        }
                    },
                    valueExpr: "value",
                    displayExpr: "text"
                },
                calculateSortValue: function (data) {
                    var value = this.calculateCellValue(data);
                    return this.lookup.calculateCellValue(value);
                }
            },
            {
                dataField: "revision_number",
                dataType: "string",
                caption: "Revision Number",
            },
            {
                dataField: "created_on",
                dataType: "date",
                caption: "Created Date",
                visible: false,
                editorOptions: {
                    disabled: true
                },
            },
            {
                dataField: "modified_on",
                dataType: "date",
                caption: "Last Modified Date",
                editorOptions: {
                    disabled: true
                },
                visible: false
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
            },
            {
                dataField: "uom_id",
                dataType: "string",
                caption: "Unit",
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: "/api/UOM/UOM/UOMIdLookup",
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
                dataField: "rkab_quantity",
                dataType: "number",
                caption: "RKAB",
                format: "fixedPoint",
                formItem: {
                    editorType: "dxNumberBox",
                    editorOptions: {
                        format: "fixedPoint",
                    }
                },
            },
            {
                dataField: "is_baseline",
                dataType: "boolean",
                caption: "Is Baseline",
            },
            {
                dataField: "is_locked",
                dataType: "boolean",
                caption: "Is Locked",
                setCellValue: function (rowData, value) {
                    // Re-trigger onEditorPreparing, so this can handle
                    // update disabled/undisabled other fields/columns
                    rowData.is_locked = value
                }
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
            {
                caption: "See Report",
                type: "buttons",
                width: 150,
                buttons: [{
                    cssClass: "btn-dxdatagrid",
                    hint: "See Contract Terms",
                    text: "Open Report",
                    onClick: function (e) {
                        salesPlanSnapshotId = e.row.data.id
                        window.location = "/Planning/SalesPlan/Report?salesPlanId=" + salesPlanSnapshotId
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

                // Sales Plan Information Container
                renderSalesPlanInformation(currentRecord, container)

                // Sales Plan Details (Monthly) Container
                renderSalesPlanMonthly(currentRecord, container)
            }
        },
        onContentReady: function (e) {
            let grid = e.component
            let queryString = window.location.search
            let params = new URLSearchParams(queryString)

            let salesPlanId = params.get("Id")

            if (salesPlanId) {
                grid.filter(["id", "=", salesPlanId])

                /* Open edit form */
                if (params.get("openEditingForm") == "true") {
                    let rowIndex = grid.getRowIndexByKey(salesPlanId)

                    grid.editRow(rowIndex)
                }
            }
        },
        onEditorPreparing: function (e) {
            if (e.parentType == "dataRow") {

                // Disabled all columns/fields if is_locked is true
                if (e.dataField !== "is_locked" && e.row.data.is_locked) {
                    e.editorOptions.disabled = true
                }
            }
        },
        onInitNewRow: function (e) {
            e.data.is_locked = false
            e.data.is_baseline = false
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

    const renderSalesPlanInformation = function (currentRecord, container) {
        let createdOnFormatted = currentRecord.created_on ? moment(currentRecord.created_on.split('T')[0]).format("D MMM YYYY") : '-'
        let modifiedOnFormatted = currentRecord.modified_on ? moment(currentRecord.modified_on.split('T')[0]).format("D MMM YYYY") : '-'

        let salesPlanInformationContainer = $(`
            <div>
                <h5 class="mb-3">Sales Plan Detail</h5>

                <div class="row mb-4">
                    <div class="col-md-6">
                        <div class="master-detail-caption mb-2">Overview</div>
                        <div class="card card-mcs card-headline">
                            <div class="row">
                                <div class="col-md-6 pr-0">
                                    <div class="headline-title-container">
                                        <small class="font-weight-normal d-block mb-1">Sales Plan Name</small>
                                        <h4 class="headline-title font-weight-bold">${(currentRecord.plan_name ? currentRecord.plan_name : "-")}</h4>
                                    </div>
                                </div>
                                <div class="col-md-6 pl-0">
                                    <div class="headline-detail-container">
                                        <div class="d-flex align-items-start mb-3">
                                            <div class="d-inline-block mr-3">
                                                <div class="icon-circle">
                                                    <i class="fas fa-th-large fa-sm"></i>
                                                </div>
                                            </div>
                                            <div class="d-inline-block">
                                                <small class="font-weight-normal text-muted d-block mb-1">Site</small>
                                                <h5 class="font-weight-bold">${(currentRecord.site_name ? currentRecord.site_name : "-")}</h5>
                                            </div>
                                        </div>
                                        <div class="d-flex align-items-start mb-3">
                                            <div class="d-inline-block mr-3">
                                                <div class="icon-circle">
                                                    <i class="fas fa-box fa-sm"></i>
                                                </div>
                                            </div>
                                            <div class="d-inline-block">
                                                <small class="font-weight-normal text-muted d-block mb-1">Revision Number</small>
                                                <h5 class="font-weight-bold">${(currentRecord.revision_number ? currentRecord.revision_number : "-")}</h5>
                                            </div>
                                        </div>
                                        <div class="d-flex align-items-start">
                                            <div class="d-inline-block mr-3">
                                                <div class="icon-circle">
                                                    <i class="fas fa-flag fa-sm"></i>
                                                </div>
                                            </div>
                                            <div class="d-inline-block">
                                                <small class="font-weight-normal text-muted d-block mb-1">Is Baseline</small>
                                                <h5 class="font-weight-bold">${(currentRecord.is_baseline ? "Yes" : "No")}</h5>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="master-detail-caption mb-2">Plan Information</div>
                        <div class="card card-mcs card-headline">
                            <div class="row">
                                <div class="col-md-12">
                                    <div class="headline-detail-container">
                                        <div class="row">
                                            <div class="col-md-6">
                                                <div class="d-flex align-items-start mb-3">
                                                    <div class="d-inline-block mr-3">
                                                        <div class="icon-circle">
                                                            <i class="fas fa-calendar-alt fa-sm"></i>
                                                        </div>
                                                    </div>
                                                    <div class="d-inline-block">
                                                        <small class="font-weight-normal text-muted d-block mb-1">Created Date</small>
                                                        <h5 class="font-weight-bold">${ createdOnFormatted }</h5>
                                                    </div>
                                                </div>
                                                <div class="d-flex align-items-start mb-3">
                                                    <div class="d-inline-block mr-3">
                                                        <div class="icon-circle">
                                                            <i class="fas fa-calendar-alt fa-sm"></i>
                                                        </div>
                                                    </div>
                                                    <div class="d-inline-block">
                                                        <small class="font-weight-normal text-muted d-block mb-1">Modified Date</small>
                                                        <h5 class="font-weight-bold">${ modifiedOnFormatted }</h5>
                                                    </div>
                                                </div>
                                                <div class="d-flex align-items-start mb-3">
                                                    <div class="d-inline-block mr-3">
                                                        <div class="icon-circle">
                                                            <i class="fas fa-calculator fa-sm"></i>
                                                        </div>
                                                    </div>
                                                    <div class="d-inline-block">
                                                        <small class="font-weight-normal text-muted d-block mb-1">Quantity</small>
                                                        <h5 class="font-weight-bold">${(currentRecord.quantity ? formatNumber(currentRecord.quantity) : "-")}</h5>
                                                    </div>
                                                </div>
                                                <div class="d-flex align-items-start">
                                                    <div class="d-inline-block mr-3">
                                                        <div class="icon-circle">
                                                            <i class="fas fa-lock fa-sm"></i>
                                                        </div>
                                                    </div>
                                                    <div class="d-inline-block">
                                                        <small class="font-weight-normal text-muted d-block mb-1">Is Locked</small>
                                                        <h5 class="font-weight-bold">${(currentRecord.is_locked ? "Yes" : "No")}</h5>
                                                    </div>
                                                </div>
                                            </div>
                                            <div class="col-md-6">
                                                <div class="d-flex align-items-start mb-3">
                                                    <div class="d-inline-block mr-3">
                                                        <div class="icon-circle">
                                                            <i class="fas fa-user fa-sm"></i>
                                                        </div>
                                                    </div>
                                                    <div class="d-inline-block">
                                                        <small class="font-weight-normal text-muted d-block mb-1">Created By</small>
                                                        <h5 class="font-weight-bold">${(currentRecord.created_by_name ? currentRecord.created_by_name : "-")}</h5>
                                                    </div>
                                                </div>
                                                <div class="d-flex align-items-start mb-3">
                                                    <div class="d-inline-block mr-3">
                                                        <div class="icon-circle">
                                                            <i class="fas fa-user fa-sm"></i>
                                                        </div>
                                                    </div>
                                                    <div class="d-inline-block">
                                                        <small class="font-weight-normal text-muted d-block mb-1">Modified By</small>
                                                        <h5 class="font-weight-bold">${(currentRecord.modified_by_name ? currentRecord.modified_by_name : "-")}</h5>
                                                    </div>
                                                </div>
                                                <div class="d-flex align-items-start mb-3">
                                                    <div class="d-inline-block mr-3">
                                                        <div class="icon-circle">
                                                            <i class="fas fa-box fa-sm"></i>
                                                        </div>
                                                    </div>
                                                    <div class="d-inline-block">
                                                        <small class="font-weight-normal text-muted d-block mb-1">Unit</small>
                                                        <h5 class="font-weight-bold">${(currentRecord.uom_name ? currentRecord.uom_name : "-")}</h5>
                                                    </div>
                                                </div>
                                                <div class="d-flex align-items-start">
                                                    <div class="d-inline-block mr-3">
                                                        <div class="icon-circle">
                                                            <i class="fas fa-list fa-sm"></i>
                                                        </div>
                                                    </div>
                                                    <div class="d-inline-block">
                                                        <small class="font-weight-normal text-muted d-block mb-1">Notes</small>
                                                        <h5 class="font-weight-bold">${(currentRecord.notes ? currentRecord.notes : "-")}</h5>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Not used -->
                <div class="row mb-5 d-none">
                    <div class="col-md-6">
                        <div class="master-detail-caption mb-2">Plan Information</div>
                        <div class="card card-mcs">
                            <dl class="row card-body">
                                <dt class="col-md-4 mb-2">Sales Plan Name</dt>
                                <dd class="col-md-8">`+ (currentRecord.plan_name ? currentRecord.plan_name : "-") + `</dd>

                                <dt class="col-md-4 mb-2">Start Date</dt>
                                <dd class="col-md-8">`+ (currentRecord.start_date ? currentRecord.start_date.split('T')[0] : "-") + `</dd>
                                        
                                <dt class="col-md-4 mb-2">End Date</dt>
                                <dd class="col-md-8">`+ (currentRecord.end_date ? currentRecord.end_date.split('T')[0] : "-") + `</dd>

                                <dt class="col-md-4 mb-2">Quantity</dt>
                                <dd class="col-md-8">`+ (currentRecord.quantity ? currentRecord.quantity : "-") + `</dd>
                                
                                <dt class="col-md-4 mb-2">Unit</dt>
                                <dd class="col-md-8">`+ (currentRecord.uom_name ? currentRecord.uom_name : "-") + `</dd>
                            </dl>
                        </div>
                    </div>
                    <div class="col-md-6">
                    </div>
                </div>
            </div>
        `).appendTo(container)
    }

    const renderSalesPlanMonthly = function (currentRecord, container) {
        var detailName = "SalesPlanDetail";
        var urlDetail = "/api/" + areaName + "/" + detailName;

        let salesPlanDetailsContainer = $("<div class='mb-5'>")
        salesPlanDetailsContainer.appendTo(container)

        $("<div>")
            .addClass("master-detail-caption mb-2")
            .text("Monthly Details")
            .appendTo(salesPlanDetailsContainer);

        $("<div>")
            .dxDataGrid({
                dataSource: DevExpress.data.AspNet.createStore({
                    key: "id",
                    loadUrl: urlDetail + "/BySalesPlanId/" + encodeURIComponent(currentRecord.id),
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
                        dataField: "sales_plan_id",
                        allowEditing: false,
                        visible: false,
                        formItem: {
                            colSpan: 2
                        },
                        calculateCellValue: function () {
                            return currentRecord.id;
                        }
                    },
                    {
                        dataField: "month_id",
                        dataType: "number",
                        caption: "Month",
                        validationRules: [{
                            type: "required",
                            message: "The field is required."
                        }],
                        lookup: {
                            dataSource: DevExpress.data.AspNet.createStore({
                                key: "value",
                                loadUrl: urlDetail + "/MonthIndexLookup",
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
                        sortOrder: "asc"
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
                        customizeText: function (cellInfo) {
                            return numeral(cellInfo.value).format('0,0.00');
                        }
                    },
                    {
                        caption: "Detail",
                        type: "buttons",
                        width: 200,
                        buttons: [{
                            cssClass: "btn-dxdatagrid",
                            hint: "See Customers",
                            text: "See Customers",
                            onClick: function (e) {
                                salesPlanDetailData = e.row.data;
                                showSalesPlanMonthlyCustomerPopup();
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
                    e.data.sales_plan_id = currentRecord.id;
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
            }).appendTo(salesPlanDetailsContainer);
    }

    let popupOptions = {
        title: "Monthly Customers",
        height: "'auto'",
        closeOnOutsideClick: true,
        contentTemplate: function () {
            let container = $("<div>")

            $(`<div class="mb-3">
                    <div class="row">
                        <div class="col-md-3">
                            <small class="font-weight-normal">Sales Plan</small>
                            <h3 class="font-weight-bold">`+ salesPlanDetailData.plan_name +`</h6>
                        </div>
                        <div class="col-md-2">
                            <small class="font-weight-normal">Month</small>
                            <h3 class="font-weight-bold">`+ salesPlanDetailData.month_name + `</h6>
                        </div>
                        <div class="col-md-3">
                            <small class="font-weight-normal">Quantity</small>
                            <h3 class="font-weight-bold">`+ formatNumber(salesPlanDetailData.quantity) + `</h6>
                        </div>
                    </div>
                </div>
            `).appendTo(container)


            let url = "/api/Planning/SalesPlanCustomer";
            $("<div>")
                .dxDataGrid({
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "id",
                        loadUrl: url + "/BySalesPlanDetailId/" + salesPlanDetailData.id,
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
                            dataField: "sales_plan_detail_id",
                            dataType: "string",
                            caption: "Sales Plan Detail (Monthly) Id",
                            allowEditing: false,
                            visible: false,
                            formItem: {
                                visible: false
                            },
                            calculateCellValue: function () {
                                return salesPlanDetailData.id;
                            }
                        },
                        {
                            dataField: "customer_id",
                            dataType: "String",
                            caption: "Customer",
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
                            setCellValue: function (rowData, value) {
                                rowData.customer_id = value;
                                CustomerId = value;
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
                            customizeText: function (cellInfo) {
                                return numeral(cellInfo.value).format('0,0.00');
                            }
                        },
                        {
                            dataField: "sales_contract_id",
                            dataType: "string",
                            caption: "Sales Contract",
                            lookup: {
                                dataSource: function (options) {
                                    return {
                                        store: DevExpress.data.AspNet.createStore({
                                            key: "value",
                                            loadUrl: url + "/SalesContractIdLookup?CustomerId=" + encodeURIComponent(CustomerId),
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
                            //editorOptions: {
                            //    onOpened: function (e) {
                            //        renderAddNewButton("/Sales/Customer/Index")

                            //        // Always reload dataSource onOpenned to get new data
                            //        let lookup = e.component
                            //        lookup._dataSource.reload()
                            //    }
                            //},
                            setCellValue: function (rowData, value) {
                                rowData.sales_contract_id = value
                            }
                        },
                        {
                            dataField: "dmo",
                            dataType: "text",
                            caption: "DMO",
                            lookup: {
                                dataSource: DevExpress.data.AspNet.createStore({
                                    key: "value",
                                    loadUrl: url + "/DMOLookup",
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
                                rowData.dmo = value;
                            }
                        },
                        {
                            dataField: "electricity",
                            dataType: "text",
                            caption: "Electricity",
                            lookup: {
                                dataSource: DevExpress.data.AspNet.createStore({
                                    key: "value",
                                    loadUrl: url + "/ElectricityLookup",
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
                                rowData.electricity = value;
                            }
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
                    height: 500,
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
                        e.data.sales_plan_detail_id = salesPlanDetailData.id;
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
                }).appendTo(container);

            return container;
        }
    }

    var popup = $("#popup").dxPopup(popupOptions).dxPopup("instance")

    const showSalesPlanMonthlyCustomerPopup = function () {
        popup.option("contentTemplate", popupOptions.contentTemplate.bind(this));
        popup.show()
    }

    $('#btnUpload').on('click', function () {
        $("#upload-indicator").attr("hidden", false);
        $("#upload-result").attr("hidden", false);

        var f = $("#fUpload")[0].files;
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
                url: "/api/Planning/SalesPlan/UploadDocument",
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