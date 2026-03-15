using Pharma.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Pharma.Models
{
    public class MostSoldMedicineViewModel
    {
        public string MedicineName { get; set; }
        public int Orders { get; set; }
        public int QuantitySold { get; set; }
        public decimal Price { get; set; }
        public int AvailableAmount { get; set; }
        public string Status { get; set; }
       
    }

}


public class CombinedReportViewModel
{
    public List<MostSoldMedicineViewModel> OnlineMedicines { get; set; }
    public List<MostSoldMedicineViewModel> InStoreMedicines { get; set; }
    public List<MostSoldMedicineViewModel> OverallMedicines { get; set; }

    public decimal TotalOnlineRevenue { get; set; }
    public decimal TotalInStoreRevenue { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int PendingOrders { get; set; }

}

public class SalesTrendsViewModel
{
    public List<MonthData> YearlyOnlineData { get; set; }
    public List<DayData> MonthlyOnlineData { get; set; }
    public List<MonthData> YearlyRetailData { get; set; }
    public List<DayData> MonthlyRetailData { get; set; }
}

public class MonthData
{
    public int Month { get; set; }
    public decimal? Online { get; set; }
    public decimal? Retail { get; set; }
}

public class DayData
{
    public int Day { get; set; }
    public decimal? Online { get; set; }
    public decimal? Retail { get; set; }
}


public class MedicineSalesViewModel
{
    public List<MonthSalesData> YearlySalesData { get; set; }
    public List<DaySalesData> MonthlySalesData { get; set; }
}

public class MonthSalesData
{
    public int Month { get; set; }
    public int OnlineSales { get; set; }
    public int RetailSales { get; set; }
    public int TotalSales => OnlineSales + RetailSales;
}

public class DaySalesData
{
    public int Day { get; set; }
    public int OnlineSales { get; set; }
    public int RetailSales { get; set; }
    public int TotalSales => OnlineSales + RetailSales;
}
