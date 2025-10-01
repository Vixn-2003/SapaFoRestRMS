using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace WebSapaForestForStaff.Models;

public partial class SapaFoRestRmsContext : DbContext
{
    public SapaFoRestRmsContext()
    {
    }

    public SapaFoRestRmsContext(DbContextOptions<SapaFoRestRmsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Announcement> Announcements { get; set; }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<BrandBanner> BrandBanners { get; set; }

    public virtual DbSet<Combo> Combos { get; set; }

    public virtual DbSet<ComboItem> ComboItems { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Event> Events { get; set; }

    public virtual DbSet<Ingredient> Ingredients { get; set; }

    public virtual DbSet<InventoryBatch> InventoryBatches { get; set; }

    public virtual DbSet<KitchenTicket> KitchenTickets { get; set; }

    public virtual DbSet<KitchenTicketDetail> KitchenTicketDetails { get; set; }

    public virtual DbSet<MarketingCampaign> MarketingCampaigns { get; set; }

    public virtual DbSet<MenuCategory> MenuCategories { get; set; }

    public virtual DbSet<MenuItem> MenuItems { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Payroll> Payrolls { get; set; }

    public virtual DbSet<PurchaseOrder> PurchaseOrders { get; set; }

    public virtual DbSet<PurchaseOrderDetail> PurchaseOrderDetails { get; set; }

    public virtual DbSet<Recipe> Recipes { get; set; }

    public virtual DbSet<Regulation> Regulations { get; set; }

    public virtual DbSet<Reservation> Reservations { get; set; }

    public virtual DbSet<ReservationTable> ReservationTables { get; set; }

    public virtual DbSet<RestaurantIntro> RestaurantIntros { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<SalaryRule> SalaryRules { get; set; }

    public virtual DbSet<Shift> Shifts { get; set; }

    public virtual DbSet<Staff> Staffs { get; set; }

    public virtual DbSet<StockTransaction> StockTransactions { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<SystemLogo> SystemLogos { get; set; }

    public virtual DbSet<Table> Tables { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Voucher> Vouchers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=GRANOSDESKTOP\\GRANOS;Database=SapaFoRestRMS;User Id=sa;Password=152003;TrustServerCertificate=True;MultipleActiveResultSets=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.HasKey(e => e.AnnouncementId).HasName("PK__Announce__9DE44574AF2CDD8F");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ExpiredAt).HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Announcements)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Announcements_Users");
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.AttendanceId).HasName("PK__Attendan__8B69261C21E60AEB");

            entity.ToTable("Attendance");

            entity.Property(e => e.CheckIn).HasColumnType("datetime");
            entity.Property(e => e.CheckOut).HasColumnType("datetime");
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(d => d.Staff).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Attendanc__Staff__0D7A0286");
        });

        modelBuilder.Entity<BrandBanner>(entity =>
        {
            entity.HasKey(e => e.BannerId).HasName("PK__BrandBan__32E86AD1C09BB9DE");

            entity.Property(e => e.ImageUrl).HasMaxLength(300);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.BrandBanners)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_BrandBanners_Users");
        });

        modelBuilder.Entity<Combo>(entity =>
        {
            entity.HasKey(e => e.ComboId).HasName("PK__Combos__DD42582E5AE34195");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<ComboItem>(entity =>
        {
            entity.HasKey(e => e.ComboItemId).HasName("PK__ComboIte__EE32F805B1728588");

            entity.HasOne(d => d.Combo).WithMany(p => p.ComboItems)
                .HasForeignKey(d => d.ComboId)
                .HasConstraintName("FK__ComboItem__Combo__0F624AF8");

            entity.HasOne(d => d.MenuItem).WithMany(p => p.ComboItems)
                .HasForeignKey(d => d.MenuItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ComboItem__MenuI__10566F31");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__A4AE64D8979D7D21");

            entity.Property(e => e.LoyaltyPoints).HasDefaultValue(0);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(d => d.User).WithMany(p => p.Customers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Customers__UserI__114A936A");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("PK__Events__7944C81052099D61");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Location).HasMaxLength(200);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Events)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Events_Users");
        });

        modelBuilder.Entity<Ingredient>(entity =>
        {
            entity.HasKey(e => e.IngredientId).HasName("PK__Ingredie__BEAEB25A5BEF19C8");

            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.ReorderLevel)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Unit).HasMaxLength(20);
        });

        modelBuilder.Entity<InventoryBatch>(entity =>
        {
            entity.HasKey(e => e.BatchId).HasName("PK__Inventor__5D55CE58BC415D80");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.QuantityRemaining).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.InventoryBatches)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Inventory__Ingre__1332DBDC");

            entity.HasOne(d => d.PurchaseOrderDetail).WithMany(p => p.InventoryBatches)
                .HasForeignKey(d => d.PurchaseOrderDetailId)
                .HasConstraintName("FK__Inventory__Purch__14270015");
        });

        modelBuilder.Entity<KitchenTicket>(entity =>
        {
            entity.HasKey(e => e.TicketId).HasName("PK__KitchenT__712CC607EFDC1BD5");

            entity.Property(e => e.CourseType).HasMaxLength(20);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.Order).WithMany(p => p.KitchenTickets)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__KitchenTi__Order__17036CC0");
        });

        modelBuilder.Entity<KitchenTicketDetail>(entity =>
        {
            entity.HasKey(e => e.TicketDetailId).HasName("PK__KitchenT__39BFBDE6F988AC07");

            entity.HasOne(d => d.OrderDetail).WithMany(p => p.KitchenTicketDetails)
                .HasForeignKey(d => d.OrderDetailId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__KitchenTi__Order__151B244E");

            entity.HasOne(d => d.Ticket).WithMany(p => p.KitchenTicketDetails)
                .HasForeignKey(d => d.TicketId)
                .HasConstraintName("FK__KitchenTi__Ticke__160F4887");
        });

        modelBuilder.Entity<MarketingCampaign>(entity =>
        {
            entity.HasKey(e => e.CampaignId).HasName("PK__Marketin__3F5E8A994C02E413");

            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.MarketingCampaigns)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_MarketingCampaigns_Users");
        });

        modelBuilder.Entity<MenuCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__MenuCate__19093A0B207E10AF");

            entity.Property(e => e.CategoryName).HasMaxLength(100);
        });

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.HasKey(e => e.MenuItemId).HasName("PK__MenuItem__8943F7224C5CE0A0");

            entity.Property(e => e.CourseType).HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Category).WithMany(p => p.MenuItems)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__MenuItems__Categ__18EBB532");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BCF499F9CB6");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.OrderType).HasMaxLength(20);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TotalAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Customer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK__Orders__Customer__1BC821DD");

            entity.HasOne(d => d.Reservation).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ReservationId)
                .HasConstraintName("FK__Orders__Reservat__1CBC4616");
        });

        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.OrderDetailId).HasName("PK__OrderDet__D3B9D36CD2722C81");

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.MenuItem).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.MenuItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderDeta__MenuI__19DFD96B");

            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__OrderDeta__Order__1AD3FDA4");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payments__9B556A382B8B89C4");

            entity.Property(e => e.DiscountAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.FinalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.PaymentMethod).HasMaxLength(20);
            entity.Property(e => e.Subtotal).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Vatamount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("VATAmount");
            entity.Property(e => e.Vatpercent)
                .HasDefaultValue(10m)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("VATPercent");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payments__OrderI__1DB06A4F");

            entity.HasOne(d => d.Voucher).WithMany(p => p.Payments)
                .HasForeignKey(d => d.VoucherId)
                .HasConstraintName("FK__Payments__Vouche__1EA48E88");
        });

        modelBuilder.Entity<Payroll>(entity =>
        {
            entity.HasKey(e => e.PayrollId).HasName("PK__Payroll__99DFC672F9C5DA92");

            entity.ToTable("Payroll");

            entity.Property(e => e.BaseSalary).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MonthYear)
                .HasMaxLength(7)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.NetSalary).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");
            entity.Property(e => e.TotalBonus).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TotalPenalty).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Staff).WithMany(p => p.Payrolls)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Payroll__StaffId__1F98B2C1");
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.PurchaseOrderId).HasName("PK__Purchase__036BACA4FEB70354");

            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.Supplier).WithMany(p => p.PurchaseOrders)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PurchaseO__Suppl__22751F6C");
        });

        modelBuilder.Entity<PurchaseOrderDetail>(entity =>
        {
            entity.HasKey(e => e.PurchaseOrderDetailId).HasName("PK__Purchase__5026B69807507117");

            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.PurchaseOrderDetails)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PurchaseO__Ingre__208CD6FA");

            entity.HasOne(d => d.PurchaseOrder).WithMany(p => p.PurchaseOrderDetails)
                .HasForeignKey(d => d.PurchaseOrderId)
                .HasConstraintName("FK__PurchaseO__Purch__2180FB33");
        });

        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.HasKey(e => e.RecipeId).HasName("PK__Recipes__FDD988B0427BB254");

            entity.Property(e => e.QuantityNeeded).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.Recipes)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Recipes__Ingredi__236943A5");

            entity.HasOne(d => d.MenuItem).WithMany(p => p.Recipes)
                .HasForeignKey(d => d.MenuItemId)
                .HasConstraintName("FK__Recipes__MenuIte__245D67DE");
        });

        modelBuilder.Entity<Regulation>(entity =>
        {
            entity.HasKey(e => e.RegulationId).HasName("PK__Regulati__A192C7E9936DD8E2");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Regulations)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Regulations_Users");
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.ReservationId).HasName("PK__Reservat__B7EE5F24F691A8C8");

            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.ReservationTime).HasColumnType("datetime");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Pending");

            entity.HasOne(d => d.Customer).WithMany(p => p.Reservations)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reservati__Custo__2645B050");
        });

        modelBuilder.Entity<ReservationTable>(entity =>
        {
            entity.HasKey(e => e.ReservationTableId).HasName("PK__Reservat__A32A179676EFD1F2");

            entity.HasIndex(e => new { e.ReservationId, e.TableId }, "UQ_Reservation_Table").IsUnique();

            entity.HasOne(d => d.Reservation).WithMany(p => p.ReservationTables)
                .HasForeignKey(d => d.ReservationId)
                .HasConstraintName("FK__Reservati__Reser__2739D489");

            entity.HasOne(d => d.Table).WithMany(p => p.ReservationTables)
                .HasForeignKey(d => d.TableId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Reservati__Table__282DF8C2");
        });

        modelBuilder.Entity<RestaurantIntro>(entity =>
        {
            entity.HasKey(e => e.IntroId).HasName("PK__Restaura__303BA93E69281BA6");

            entity.ToTable("RestaurantIntro");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.RestaurantIntros)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_RestaurantIntro_Users");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1A250DF799");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B616007B6F3C9").IsUnique();

            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<SalaryRule>(entity =>
        {
            entity.HasKey(e => e.RuleId).HasName("PK__SalaryRu__110458E23D94B510");

            entity.Property(e => e.BaseWorkDays).HasDefaultValue(26);
            entity.Property(e => e.BonusPerShift)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.FullSalaryCondition).HasDefaultValue(26);
            entity.Property(e => e.PenaltyAbsent)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.Property(e => e.PenaltyLate)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<Shift>(entity =>
        {
            entity.HasKey(e => e.ShiftId).HasName("PK__Shifts__C0A8388150592529");

            entity.Property(e => e.EndTime).HasColumnType("datetime");
            entity.Property(e => e.StartTime).HasColumnType("datetime");

            entity.HasOne(d => d.Staff).WithMany(p => p.Shifts)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Shifts__StaffId__2A164134");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(e => e.StaffId).HasName("PK__Staffs__96D4AB170E2EA873");

            entity.Property(e => e.Position).HasMaxLength(50);
            entity.Property(e => e.SalaryBase).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.User).WithMany(p => p.Staff)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Staffs__UserId__2B0A656D");
        });

        modelBuilder.Entity<StockTransaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__StockTra__55433A6B0213684E");

            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.Quantity).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.TransactionDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Type).HasMaxLength(20);

            entity.HasOne(d => d.Batch).WithMany(p => p.StockTransactions)
                .HasForeignKey(d => d.BatchId)
                .HasConstraintName("FK_StockTransactions_Batch");

            entity.HasOne(d => d.Ingredient).WithMany(p => p.StockTransactions)
                .HasForeignKey(d => d.IngredientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__StockTran__Ingre__2BFE89A6");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.SupplierId).HasName("PK__Supplier__4BE666B4178F6CB0");

            entity.Property(e => e.Address).HasMaxLength(200);
            entity.Property(e => e.ContactInfo).HasMaxLength(200);
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
        });

        modelBuilder.Entity<SystemLogo>(entity =>
        {
            entity.HasKey(e => e.LogoId).HasName("PK__SystemLo__C620158D901C32AB");

            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LogoName).HasMaxLength(200);
            entity.Property(e => e.UpdatedDate).HasColumnType("datetime");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.SystemLogos)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_SystemLogos_Users");
        });

        modelBuilder.Entity<Table>(entity =>
        {
            entity.HasKey(e => e.TableId).HasName("PK__Tables__7D5F01EE70EE0820");

            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Available");
            entity.Property(e => e.TableNumber).HasMaxLength(20);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4CF48ECA77");

            entity.HasIndex(e => e.Email, "UQ__Users__A9D105347A19B095").IsUnique();

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__RoleId__2EDAF651");
        });

        modelBuilder.Entity<Voucher>(entity =>
        {
            entity.HasKey(e => e.VoucherId).HasName("PK__Vouchers__3AEE7921CBF3A9F4");

            entity.HasIndex(e => e.Code, "UQ__Vouchers__A25C5AA723335369").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.DiscountType).HasMaxLength(20);
            entity.Property(e => e.DiscountValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MaxDiscount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.MinOrderValue).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("Active");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
