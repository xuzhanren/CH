using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CanHappy.Data.Migrations
{
    /// <inheritdoc />
    public partial class SetBuySellCategoryKeywords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""Category""
                SET ""KeyWords"" = 'buy sell sale computer laptop used new refurbished Camera Camcorders Furniture desk table chair bed box sofa music Arts Music Home  Appliances Cell Phones Tools Building Materials Sports Garden Plants Kids Toys game Industrial  Equipment Samsung iPhone fridge refridgerator washer dryer stove range electronics TV bag luggage case LG Fridgedare parts'
                WHERE ""Name"" = 'Buy & Sell';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""Category""
                SET ""KeyWords"" = NULL
                WHERE ""Name"" = 'Buy & Sell';");
        }
    }
}
