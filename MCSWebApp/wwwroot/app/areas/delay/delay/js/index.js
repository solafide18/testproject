$(function () {

    var token = $.cookie("Token");
    var areaName = "Delay";
    var entityName = "Delay";
    var url = "/api/" + areaName + "/" + entityName;
    const maxFileSize = 52428800;
    var headerRecord;

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
                dataField: "delay_date",
                dataType: "date",
                caption: "Date",
                validationRules: [{
                    type: "required",
                    message: "This field is required."
                }]
            },

            {
                dataField: "business_area_id",
                dataType: "text",
                caption: "Business Area",
                validationRules: [{
                    type: "required",
                    message: "The Business Area field is required."
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
                }
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
                        loadUrl: "/api/Equipment/EquipmentList/EquipmentTypeIdLookup",
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
                dataField: "equipment_id",
                dataType: "text",
                caption: "Equipment",
                validationRules: [{
                    type: "required",
                    message: "The Equipment field is required."
                }],
                lookup: {
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "value",
                        loadUrl: "/api/equipment/equipmentlist/EquipmentIdLookup",
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
                dataField: "record_created_by",
                dataType: "text",
                caption: "CreatedBy",
                editorOptions: { readOnly: true }
            },
            {
                dataField: "remark",
                editorType: "dxTextArea",
                editorOptions: {
                    height: 80
                },
                colSpan: 2
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
        height: 800,
        showBorders: true,
        editing: {
            mode: "form",
            allowAdding: true,
            allowUpdating: true,
            allowDeleting: true,
            useIcons: true,
            form: {
                itemType: "group",
                items: [
                    {
                        dataField: "delay_date",
                        colSpan: 2,
                    },
                    {
                        dataField: "business_area_id",
                        colSpan: 2,
                    },
                    {
                        dataField: "equipment_type_id",
                        colSpan: 2,
                    },
                    {
                        dataField: "equipment_id",
                        colSpan: 2
                    },
                    {
                        dataField: "remark",
                        colSpan: 2
                    }
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
            if (e.parentType === "dataRow" && e.dataField == "despatch_order_id") {

                let standardHandler = e.editorOptions.onValueChanged
                let index = e.row.rowIndex
                let grid = e.component
                let rowData = e.row.data

                e.editorOptions.onValueChanged = async function (e) { // Overiding the standard handler                    

                    // Get its value (Id) on value changed
                    let despatchOrderId = e.value

                    grid.beginCustomLoading()

                    // Get another data from API after getting the Id
                    $.ajax({
                        url: '/api/Sales/DespatchOrder/DataDetail?Id=' + despatchOrderId,
                        type: 'GET',
                        contentType: "application/json",
                        beforeSend: function (xhr) {
                            xhr.setRequestHeader("Authorization", "Bearer " + token);
                        },
                        success: function (response) {
                            let despatchOrderData = response.data;

                            grid.beginUpdate()
                            // Set its corresponded field's value
                            grid.cellValue(index, "surveyor_id", despatchOrderData.surveyor_id)
                            grid.cellValue(index, "product_id", despatchOrderData.product_id)
                            grid.endUpdate()
                        }
                    })

                    setTimeout(() => {
                        grid.endCustomLoading()
                    }, 500)

                    standardHandler(e) // Calling the standard handler to save the edited value
                }
            }
        },
        onInitNewRow: function (e) {
            e.data.is_draft_survey = false;
        },
        onEditingStart: function (e) {
            if (e.data !== null && e.data.approved_on !== null) {
                e.cancel = true;
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
                    title: "Details",
                    template: createDetailsTab(masterDetailOptions.data)
                },
                //{
                //    title: "Documents",
                //    template: createDocumentsTab(masterDetailOptions.data)
                //},
            ]
        });
    }

    function createDetailsTab(masterDetailData) {
        return function () {
            let currentRecord = masterDetailData;
            let urlDetail = "/api/DelayDetails/DelayDetails";

            return $("<div>")
                .dxDataGrid({
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "id",
                        loadUrl: urlDetail + "/DataGrid?delayId=" + encodeURIComponent(currentRecord.id),
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
                            dataField: "start_date",
                            dataType: "datetime",
                            caption: "Start Date",
                        },
                        {
                            dataField: "end_date",
                            dataType: "datetime",
                            caption: "End Date",
                        },
                        {
                            dataField: "event_definition_category_id",
                            dataType: "text",
                            caption: "Type",
                            validationRules: [{
                                type: "required",
                                message: "The Event Definition Category is required."
                            }],
                            lookup: {
                                dataSource: DevExpress.data.AspNet.createStore({
                                    key: "value",
                                    loadUrl: "/api/General/EventDefinitionCategory/EventDefinitionCategoryIdLookup",
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
                                rowData.event_definition_category_id = value;
                                //rowData.event_category_id = null;
                                //rowData.activity_code = null;
                            },
                        },
                        {
                            dataField: "event_category_id",
                            dataType: "text",
                            caption: "Problem Reported",
                            //validationRules: [{
                            //    type: "required",
                            //    message: "Activity is required."
                            //}],
                            editorOptions: { disabled: true },
                            lookup: {
                                dataSource: function (options) {
                                    return {
                                        store: DevExpress.data.AspNet.createStore({
                                            key: "value",
                                            loadUrl: "/api/General/EventCategory/EventCategoryIdLookupFilterByEventDefinitionCategoryId",
                                            onBeforeSend: function (method, ajaxOptions) {
                                                ajaxOptions.xhrFields = { withCredentials: true };
                                                ajaxOptions.beforeSend = function (request) {
                                                    request.setRequestHeader("Authorization", "Bearer " + token);
                                                };
                                            }
                                        }),
                                        filter: options.data ? ["event_definition_category_id", "=", options.data.event_definition_category_id] : null
                                    }
                                },
                                valueExpr: "value",
                                displayExpr: "text"
                            },
                            formItem: {
                                editorOptions: {
                                    showClearButton: true
                                }
                            }
                        },
                        {
                            dataField: "master_list_id",
                            dataType: "text",
                            caption: "Delay Information",
                            lookup: {
                                dataSource: DevExpress.data.AspNet.createStore({
                                    key: "value",
                                    loadUrl: "/api/General/MasterList/MasterListIdLookupByItemGroup?itemGroup=delay-information",
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
                            dataField: "remark",
                            editorType: "dxTextArea",
                            editorOptions: {
                                height: 100
                            },
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
                    showBorders: true,
                    editing: {
                        mode: "form",
                        allowAdding: true,
                        allowUpdating: true,
                        allowDeleting: true,
                        useIcons: true,
                        form: {
                            itemType: "group",
                            items: [
                                {
                                    dataField: "start_date",
                                    colSpan: 1,
                                },
                                {
                                    dataField: "end_date",
                                    colSpan: 1,
                                },
                                {
                                    dataField: "event_definition_category_id",
                                    colSpan: 2
                                },
                                {
                                    dataField: "event_category_id",
                                    colSpan: 2
                                },
                                {
                                    dataField: "master_list_id",
                                    colSpan: 2
                                },
                                {
                                    dataField: "remark",
                                    colSpan: 2
                                }
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
                        e.data.delay_id = currentRecord.id;
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

    let documentDataGrid;
    function createDocumentsTab(masterDetailData) {
        return function () {
            let currentRecord = masterDetailData;
            let detailName = "DraftSurveyDocument";
            let urlDetail = "/api/" + areaName + "/" + detailName;
            headerRecord = currentRecord;

            documentDataGrid = $("<div>")
                .dxDataGrid({
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "id",
                        loadUrl: urlDetail + "/ByHeaderId/" + encodeURIComponent(currentRecord.id),
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
                            dataField: "draft_survey_id",
                            allowEditing: false,
                            visible: false,
                            calculateCellValue: function () {
                                return currentRecord.id;
                            }
                        },
                        {
                            dataField: "filename",
                            dataType: "string",
                            caption: "File Name"
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
                                    xhr.open("GET", "/api/SurveyManagement/DraftSurveyDocument/DownloadDocument/" + documentData.id, true)
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
                        e.data.draft_survey_id = currentRecord.id;
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
                    draft_survey_id: headerRecord.id,
                    file: "",
                },
                colCount: 2,
                items: [
                    {
                        dataField: "draft_survey_id",
                        visible: false
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

                                    if (fileSize == 0) {
                                        alert("File content is empty.")
                                        return;
                                    }
                                    if (fileSize >= maxFileSize) {
                                        alert("File size exceeds 50 MB.");
                                        return;
                                    }

                                    let newFormData = {
                                        "draft_survey_id": formData.draft_survey_id,
                                        "fileName": fileName,
                                        "fileSize": fileSize,
                                        "data": data
                                    }

                                    $.ajax({
                                        url: `/api/${areaName}/DraftSurveyDocument/InsertData`,
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

    function createQualitySamplingTab(masterDetailData) {
        return function () {
            let currentRecord = masterDetailData;
            let detailName = "DraftSurvey";
            let urlDetail = "/api/" + areaName + "/" + detailName;
            bargingTransactionData = currentRecord

            documentDataGrid = $("<div>")
                .dxDataGrid({
                    dataSource: DevExpress.data.AspNet.createStore({
                        key: "id",
                        loadUrl: urlDetail + "/ByDraftSurveyId/" + encodeURIComponent(currentRecord.id),
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