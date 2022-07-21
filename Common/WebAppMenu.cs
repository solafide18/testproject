using System;
using System.Collections.Generic;
using System.Text;

namespace Common
{
    public static class WebAppMenu
    {
        public static Dictionary<string, string> BreadcrumbText = new Dictionary<string, string>();

        #region Production and Logistics

        public const string ProductionLogistics = "1";
        public const string Modelling = "1.1";
        public const string Location = "1.1.1";
        public const string BusinessArea = "1.1.1.1";
        public const string MineLocation = "1.1.1.2";
        public const string StockpileLocation = "1.1.1.3";
        public const string PortLocation = "1.1.1.4";
        public const string WasteLocation = "1.1.1.5";

        public const string ProcessFlow = "1.1.2";
        public const string ProcessingCategory = "1.1.2.1";
        public const string ProcessingFlow = "1.1.2.2";

        public const string Planning = "1.2";
        public const string ProductionPlan = "1.2.1";
        public const string SalesPlan = "1.2.2";
        public const string SalesPlanSnapshot = "1.2.3";
        public const string BlendingPlan = "1.2.4";
        public const string ShippingPlan = "1.2.5";
        public const string ShipmentPlan = "1.2.6";
        public const string DrillBlastPlan = "1.2.7";
        public const string ExplosiveUsagePlan = "1.2.8";
        public const string TimesheetPlan = "1.2.9";
        public const string HaulingPlan = "1.2.10";
        public const string BargingPlan = "1.2.11";

        public const string Mining = "1.3";
        public const string Production = "1.3.1";
        public const string WasteRemoval = "1.3.2";
        public const string Processing = "1.3.3";
        public const string Hauling = "1.3.4";
        public const string Railing = "1.3.5";
        public const string Timesheet = "1.3.6";
        public const string ProductionClosing = "1.3.8";

        public const string Handling = "1.4";
        public const string Rehandling = "1.4.1";

        public const string Port = "1.5";
        public const string PortBarging = "1.5.1";
        public const string BargeLoading = "1.5.1.1";
        public const string BargeUnloading = "1.5.1.2";
        public const string PortShipping = "1.5.2";
        public const string ShipLoading = "1.5.2.1";
        public const string ShipUnloading = "1.5.2.2";
        public const string COW = "1.5.2.3";
        public const string PortDespatchDemurrage = "1.5.3";
        public const string StatementOfFact = "1.5.3.1";

        public const string Equipment = "1.6";        
        public const string EquipmentList = "1.6.1";
        public const string EquipmentType = "1.6.2";
        public const string EquipmentRate = "1.6.3";
        public const string EquipmentUsageTransaction = "1.6.4";
        public const string EquipmentIncident = "1.6.5";

        public const string Contractor = "1.8";
        public const string StockpileManagement = "1.8.1";
        public const string Reserve = "1.8.1.1";
        public const string StockpileState = "1.8.1.3";
        public const string Blending = "1.8.1.4";

        public const string QualitySampling = "1.8.2";
        public const string AssayTransaction = "1.8.2.1";

        public const string DailyRecord = "1.8.3";
        public const string Rainfall = "1.8.3.1";
        public const string TideandWave = "1.8.3.2";
        public const string PriceIndexHistory = "1.8.3.3";
        public const string Haze = "1.8.3.4";
        public const string Slippery = "1.8.3.5";
        public const string Daywork = "1.8.3.6";
        public const string DayworkClosing = "1.8.3.7";

        public const string IHub = "1.8.4";
        public const string Weighbridge = "1.8.4.1";

        public const string SurveyManagement = "1.8.5";
        public const string JointSurvey = "1.8.5.1";
        public const string StockpileSurvey = "1.8.5.3";

        #endregion

        #region Sales & Marketing

        public const string SalesMarketing = "2";
        public const string Customer = "2.1";
        public const string SalesOrder = "2.1.3";

        //public const string ShippingPlan = "2.2";
        public const string Barging = "2.3";
        public const string Documents = "2.4";
        public const string Pricing = "2.5";        

        #endregion

        #region Contract Management

        public const string ContractManagement              = "6";

        public const string Contract                        = "6.1";
        public const string SalesContract                   = "6.1.1";        
        public const string DespatchOrder                   = "6.1.2";
        public const string AdvanceContract                 = "6.1.3";
        public const string AdvanceContractIndex            = "6.1.3.1";
        public const string AdvanceContractItemDetail       = "6.1.3.2.1";
        public const string AdvanceContractItem             = "6.1.3.2";        
        public const string AdvanceContractCharge           = "6.1.3.3";
        public const string AdvanceContractChargeDetail     = "6.1.3.3.1";
        public const string AdvanceContractReference        = "6.1.3.3.2";
        public const string AdvanceContractReferenceNumber  = "6.1.3.4";
        public const string AdvanceContractValuation        = "6.1.3.5";
        public const string ProgressClaim                   = "6.1.4";
        public const string SalesCharge                     = "6.1.6";
        public const string DespatchDemurrageContract       = "6.1.7";
        public const string ShippingInstruction             = "6.1.8";

        public const string Invoice                         = "6.2";
        public const string SalesInvoice                    = "6.2.1";
        public const string DesDemValuation                 = "6.2.2";
        public const string ShippingCost                    = "6.2.3";
        public const string DesDemDebitCreditNote           = "7.0";

        #endregion

        #region Accounting Management

        public const string AccountingManagement = "3";
        public const string ChartofAccount = "3.1";
        public const string Accounts = "3.1.1";
        public const string SubAccounts = "3.1.2";

        public const string CostCenter = "3.2";
        public const string CostDistribution = "3.3";
        public const string FormulaResult = "3.3.1";
        public const string Formula = "3.3.1.1";
        public const string Result = "3.3.1.2";

        public const string MiningOperationDashboard = "3.4";
        public const string AccountingPeriod = "3.5";

        #endregion

        #region Master Data

        public const string MasterData              = "4";
        public const string Material                = "4.1";
        public const string ProductCategory         = "4.1.1";
        public const string Product                 = "4.1.2";
        public const string WasteCategory           = "4.1.3";
        public const string Waste                   = "4.1.4";
        public const string OtherMaterial           = "4.1.5";

        public const string Quality                 = "4.2";
        public const string AnalyteDefinitions      = "4.2.1";
        public const string SamplingTemplate        = "4.2.2";

        //public const string Equipment = "4.3";

        public const string Incident                = "4.4";
        public const string IncidentDefinition      = "4.4.1";
        public const string EventDefinitionCategory = "4.4.2";
        public const string EventDefinition         = "4.4.2.1";

        public const string Transport               = "4.5";
        public const string Truck                   = "4.5.1";
        public const string Train                   = "4.5.2";
        public const string Tug                     = "4.5.3";
        public const string Barge                   = "4.5.4";
        public const string Vessel                  = "4.5.5";

        public const string Organization            = "4.6";
        public const string Shift                   = "4.6.1";
        public const string ShiftCategory           = "4.6.2";
        public const string Company                 = "4.6.3";

        public const string UOM                     = "4.7";
        public const string UOMCategory             = "4.7.1";

        public const string DocumentType            = "4.8";
        public const string Bank                    = "4.9";
        public const string Currency                = "4.10";
        public const string AdministrativeArea      = "4.11";
        public const string Country                 = "4.11.1";
        public const string Province                = "4.11.2";
        public const string City                    = "4.11.3";
        public const string DataPairList            = "4.12";
        public const string CustomerType            = "4.13";
        public const string PriceIndex              = "4.14";

        public const string Calendar                = "4.16";
        public const string CurrencyExchange        = "4.18";
        public const string Tax                     = "4.21";
        public const string BankAccount             = "4.22";
        public const string MasterList              = "4.23";
        public const string BenchmarkPriceEditor    = "4.24";
        public const string ReferencePriceEditor    = "4.25";
        public const string BenchmarkPriceBrand     = "4.26";

        public const string Operator = "4.27";

        #endregion

        #region User Security Management

        public const string UserSecurityManagement = "5";
        public const string SystemAdministration = "5.1";        
        public const string ApplicationRole = "5.1.4";
        public const string ApplicationUser = "5.1.3";
        public const string Team = "5.1.2";
        public const string SAOrganization = "5.1.1";

        #endregion

        #region Reports

        public const string Reports = "9";
        public const string ReportTemplate = "9.0";
        public const string ReportSmartMining = "9.1";
        public const string RShipment = "9.1.1";
        public const string RBarging = "9.1.2";
        public const string RSof = "9.1.3";        

        public const string Ellipse = "9.2";
        public const string EllipseInvoice = "9.2.1";

        public const string ReportViewer = "9.3";

        public const string FastReport = "9.9";
        public const string FastReportAll = "9.9.1";

        #endregion

        static WebAppMenu()
        {
            BreadcrumbText.Add(ProductionLogistics, "Production & Logistics");
            BreadcrumbText.Add(Modelling, "Modelling");
            BreadcrumbText.Add(Location, "Location");
            BreadcrumbText.Add(BusinessArea, "Business Area");
            BreadcrumbText.Add(MineLocation, "Mine Location");
            BreadcrumbText.Add(StockpileLocation, "Stockpile Location");
            BreadcrumbText.Add(PortLocation, "Port Location");
            BreadcrumbText.Add(WasteLocation, "Waste Location");
            BreadcrumbText.Add(ProcessFlow, "Process Flow");
            BreadcrumbText.Add(ProcessingCategory, "Processing Category");
            BreadcrumbText.Add(ProcessingFlow, "Process Flow");
            BreadcrumbText.Add(Planning, "Planning");
            BreadcrumbText.Add(ProductionPlan, "Production Plan");
            BreadcrumbText.Add(SalesPlan, "Sales Plan");
            BreadcrumbText.Add(HaulingPlan, "Hauling Plan");
            BreadcrumbText.Add(BargingPlan, "Barging Plan");
            BreadcrumbText.Add(SalesPlanSnapshot, "Sales Plan Snapshot");
            BreadcrumbText.Add(BlendingPlan, "Blending");
            BreadcrumbText.Add(ShippingPlan, "Shipping");
            BreadcrumbText.Add(ShipmentPlan, "Shipment Plan");

            BreadcrumbText.Add(Mining, "Mining");
            BreadcrumbText.Add(Production, "Production");
            BreadcrumbText.Add(WasteRemoval, "Waste Removal");
            BreadcrumbText.Add(Processing, "Processing");
            BreadcrumbText.Add(Hauling, "Hauling");
            BreadcrumbText.Add(Railing, "Railing");
            BreadcrumbText.Add(Timesheet, "Timesheet");
            BreadcrumbText.Add(TimesheetPlan, "Daily Plan");
            BreadcrumbText.Add(DrillBlastPlan, "Drill & Blast Plan");
            BreadcrumbText.Add(ExplosiveUsagePlan, "Explosive Usage Plan");

            BreadcrumbText.Add(Handling, "Handling");
            BreadcrumbText.Add(Rehandling, "Rehandling");

            BreadcrumbText.Add(Port, "Port");
            BreadcrumbText.Add(PortBarging, "Barging");
            BreadcrumbText.Add(BargeLoading, "Loading");
            BreadcrumbText.Add(BargeUnloading, "Unloading");
            BreadcrumbText.Add(PortShipping, "Shipping");
            BreadcrumbText.Add(ShipLoading, "Loading");
            BreadcrumbText.Add(ShipUnloading, "Unloading");
            BreadcrumbText.Add(PortDespatchDemurrage, "Despatch-Demurrage");
            BreadcrumbText.Add(StatementOfFact, "Statement of Fact");

            BreadcrumbText.Add(Equipment, "Equipment");
            BreadcrumbText.Add(EquipmentList, "Equipment List");
            BreadcrumbText.Add(EquipmentType, "Equipment Type");
            BreadcrumbText.Add(EquipmentRate, "Equipment Rate");
            BreadcrumbText.Add(EquipmentUsageTransaction, "Equipment Usage");
            BreadcrumbText.Add(EquipmentIncident, "Equipment Incident");

            #region Contract Management

            BreadcrumbText.Add(ContractManagement, "Contract Management");

            BreadcrumbText.Add(Contract, "Contract");
            BreadcrumbText.Add(SalesContract, "Sales Contract");
            BreadcrumbText.Add(AdvanceContract, "Advance Contract");
            BreadcrumbText.Add(AdvanceContractIndex, "Advance Contract");
            BreadcrumbText.Add(AdvanceContractItem, "Advance Contract Item");
            BreadcrumbText.Add(AdvanceContractCharge, "Advance Contract Charge");
            BreadcrumbText.Add(AdvanceContractReference, "Advance Contract Reference");
            BreadcrumbText.Add(AdvanceContractValuation, "Advance Contract Valuation");
            BreadcrumbText.Add(AdvanceContractChargeDetail, "Advance Contract Charge Detail");
            BreadcrumbText.Add(AdvanceContractReferenceNumber, "Advance Contract Reference Number");
            BreadcrumbText.Add(ProgressClaim, "Progress Claim");
            BreadcrumbText.Add(DespatchDemurrageContract, "DesDem Contract");
            BreadcrumbText.Add(SalesOrder, "Sales Order");
            BreadcrumbText.Add(DespatchOrder, "Despatch Order");
            BreadcrumbText.Add(ShippingInstruction, "Shipping Instruction");
            BreadcrumbText.Add(SalesCharge, "Sales Charge");

            BreadcrumbText.Add(SalesInvoice, "Sales Invoice");
            BreadcrumbText.Add(DesDemValuation, "DesDem Valuation");
            BreadcrumbText.Add(DesDemDebitCreditNote, "Debit/Credit Note");
            BreadcrumbText.Add(ShippingCost, "Shipping Cost");

            #endregion

            BreadcrumbText.Add(Contractor, "Contractor");
            BreadcrumbText.Add(StockpileManagement, "Stockpile Management");
            BreadcrumbText.Add(Reserve, "Reserve");
            BreadcrumbText.Add(StockpileState, "Stockpile State");
            BreadcrumbText.Add(Blending, "Blending");

            BreadcrumbText.Add(QualitySampling, "Quality Sampling");
            BreadcrumbText.Add(AssayTransaction, "Assay Transaction");

            BreadcrumbText.Add(DailyRecord, "Daily Record");
            BreadcrumbText.Add(Rainfall, "Rainfall");
            BreadcrumbText.Add(TideandWave, "Tide and Wave");
            BreadcrumbText.Add(PriceIndexHistory, "Price Index History");
            BreadcrumbText.Add(Haze, "Haze");
            BreadcrumbText.Add(Slippery, "Slippery");
            BreadcrumbText.Add(Daywork, "Daywork");
            BreadcrumbText.Add(DayworkClosing, "Daywork Closing");
            BreadcrumbText.Add(ProductionClosing, "Production Closing");

            BreadcrumbText.Add(IHub, "I-Hub");
            BreadcrumbText.Add(Weighbridge, "Weighbridge");

            BreadcrumbText.Add(SurveyManagement, "Survey");
            BreadcrumbText.Add(JointSurvey, "Joint Survey");
            BreadcrumbText.Add(COW, "COW");
            BreadcrumbText.Add(StockpileSurvey, "Stockpile Survey");

            BreadcrumbText.Add(SalesMarketing, "Sales & Marketing");
            BreadcrumbText.Add(Customer, "Customer");

            //BreadcrumbText.Add(ShippingPlan, "Shipping Plan");
            BreadcrumbText.Add(Barging, "Barging");
            BreadcrumbText.Add(Documents, "Documents");
            BreadcrumbText.Add(Pricing, "Pricing");
            BreadcrumbText.Add(Invoice, "Invoice");

            BreadcrumbText.Add(AccountingManagement, "Accounting Management");
            BreadcrumbText.Add(ChartofAccount, "Chart of Account");
            BreadcrumbText.Add(Accounts, "Accounts");
            BreadcrumbText.Add(SubAccounts, "Sub Accounts");

            BreadcrumbText.Add(CostCenter, "Cost Center");
            BreadcrumbText.Add(CostDistribution, "Cost Distribution");
            BreadcrumbText.Add(FormulaResult, "Formula Result");
            BreadcrumbText.Add(Formula, "Formula");
            BreadcrumbText.Add(Result, "Result");

            BreadcrumbText.Add(MiningOperationDashboard, "Mining Operation Dashboard");
            BreadcrumbText.Add(AccountingPeriod, "Accounting Period");

            BreadcrumbText.Add(MasterData, "Master Data");
            BreadcrumbText.Add(Material, "Material");
            BreadcrumbText.Add(ProductCategory, "Product Category");
            BreadcrumbText.Add(Product, "Product");
            BreadcrumbText.Add(WasteCategory, "Waste Category");
            BreadcrumbText.Add(Waste, "Waste");
            BreadcrumbText.Add(OtherMaterial, "Other Material");

            BreadcrumbText.Add(Quality, "Quality");
            BreadcrumbText.Add(AnalyteDefinitions, "Analyte Definitions");
            BreadcrumbText.Add(SamplingTemplate, "Sampling Template");

            BreadcrumbText.Add(Incident, "Incident");
            BreadcrumbText.Add(IncidentDefinition, "Incident Definition");
            BreadcrumbText.Add(EventDefinition, "Event Definition");
            BreadcrumbText.Add(EventDefinitionCategory, "Event Definition Category");

            BreadcrumbText.Add(Transport, "Transport");
            BreadcrumbText.Add(Truck, "Truck");
            BreadcrumbText.Add(Train, "Train");
            BreadcrumbText.Add(Tug, "Tug");
            BreadcrumbText.Add(Barge, "Barge");
            BreadcrumbText.Add(Vessel, "Vessel");

            BreadcrumbText.Add(Organization, "Organization");
            BreadcrumbText.Add(Shift, "Shift");
            BreadcrumbText.Add(Operator, "Operator");
            BreadcrumbText.Add(ShiftCategory, "Shift Category");
            BreadcrumbText.Add(Company, "Company");

            BreadcrumbText.Add(UOM, "UOM");
            BreadcrumbText.Add(UOMCategory, "UOM Category");

            BreadcrumbText.Add(DocumentType, "Document Type");
            BreadcrumbText.Add(Bank, "Bank");
            BreadcrumbText.Add(Currency, "Currency");
            BreadcrumbText.Add(AdministrativeArea, "Region");
            BreadcrumbText.Add(City, "City");
            BreadcrumbText.Add(Province, "Province");
            BreadcrumbText.Add(Country, "Country");
            BreadcrumbText.Add(DataPairList, "Data Pair List");
            BreadcrumbText.Add(CustomerType, "Customer Type");
            BreadcrumbText.Add(PriceIndex, "Price Index");
            BreadcrumbText.Add(Calendar, "Calendar");
            BreadcrumbText.Add(CurrencyExchange, "Currency Exchange");
            BreadcrumbText.Add(Tax, "Tax");
            BreadcrumbText.Add(BankAccount, "Bank Account");
            BreadcrumbText.Add(MasterList, "Master List");
            BreadcrumbText.Add(BenchmarkPriceEditor, "Benchmark Price Editor");
            BreadcrumbText.Add(BenchmarkPriceBrand, "Benchmark Price Brand");
            BreadcrumbText.Add(ReferencePriceEditor, "Reference Price Editor");

            BreadcrumbText.Add(UserSecurityManagement, "User Security Management");
            BreadcrumbText.Add(SystemAdministration, "System Administration");
            BreadcrumbText.Add(ApplicationRole, "Application Role");
            BreadcrumbText.Add(ApplicationUser, "Application User");
            BreadcrumbText.Add(Team, "Team");            
            BreadcrumbText.Add(SAOrganization, "Organization");

            BreadcrumbText.Add(Reports, "Reports");
            BreadcrumbText.Add(ReportTemplate, "Report Template");
            BreadcrumbText.Add(ReportViewer, "Report Viewer");
            BreadcrumbText.Add(ReportSmartMining, "Smart Mining");
            BreadcrumbText.Add(RShipment, "Shipment");
            BreadcrumbText.Add(RBarging, "Barging");
            BreadcrumbText.Add(RSof, "Statement of Fact");            

            BreadcrumbText.Add(Ellipse, "Ellipse");
            BreadcrumbText.Add(EllipseInvoice, "Invoice");

            BreadcrumbText.Add(FastReport, "FastReport");
            BreadcrumbText.Add(FastReportAll, "FastReport All");
        }
    }
}
