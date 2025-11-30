# 📚 TÀI LIỆU MÔ TẢ DESIGN PATTERNS TRONG SAPAFORESTRMS API

**Ngày tạo:** 2025-01-15  
**Phiên bản:** 1.0  
**Dự án:** SapaFoRestRMS Backend API

---

## 📋 MỤC LỤC

1. [Tổng quan kiến trúc](#1-tổng-quan-kiến-trúc)
2. [Design Patterns được sử dụng](#2-design-patterns-được-sử-dụng)
3. [Chi tiết từng Pattern](#3-chi-tiết-từng-pattern)
4. [Sơ đồ kiến trúc](#4-sơ-đồ-kiến-trúc)
5. [Ví dụ code](#5-ví-dụ-code)

---

## 1. TỔNG QUAN KIẾN TRÚC

Project sử dụng **Layered Architecture (Kiến trúc phân lớp)** với 4 layers chính:

```
┌─────────────────────────────────────┐
│   Presentation Layer (Controllers)   │
│   - API Controllers                  │
│   - HTTP Request/Response handling   │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│   Business Logic Layer (Services)    │
│   - Business rules                   │
│   - DTOs mapping                     │
│   - Validation logic                 │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│   Data Access Layer (Repositories)   │
│   - Database operations              │
│   - Entity queries                  │
│   - Unit of Work                    │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│   Domain Layer (Models)              │
│   - Entity models                    │
│   - Enums                           │
│   - Domain logic                    │
└─────────────────────────────────────┘
```

---

## 2. DESIGN PATTERNS ĐƯỢC SỬ DỤNG

### ✅ **Các Patterns chính:**

1. **Repository Pattern** ⭐⭐⭐
2. **Unit of Work Pattern** ⭐⭐⭐
3. **Dependency Injection Pattern** ⭐⭐⭐
4. **Service Layer Pattern** ⭐⭐⭐
5. **DTO Pattern (Data Transfer Object)** ⭐⭐
6. **Generic Repository Pattern** ⭐⭐
7. **Factory Pattern** (implicit trong DI) ⭐
8. **Strategy Pattern** (implicit trong Services) ⭐
9. **Mapper Pattern (AutoMapper)** ⭐⭐

---

## 3. CHI TIẾT TỪNG PATTERN

### 3.1. REPOSITORY PATTERN ⭐⭐⭐

**Mục đích:** Tách biệt logic truy cập dữ liệu khỏi business logic, cung cấp abstraction layer cho database operations.

**Cấu trúc:**

```
IRepository<T> (Interface)
    ↓
Repository<T> (Generic Base Class)
    ↓
Specific Repositories (UserRepository, RoleRepository, ...)
```

**Vị trí trong code:**
- `DataAccessLayer/Repositories/Interfaces/IRepository.cs`
- `DataAccessLayer/Repositories/Repository.cs`
- `DataAccessLayer/Repositories/UserRepository.cs`
- `DataAccessLayer/Repositories/RoleRepository.cs`
- ... (22+ repositories)

**Ví dụ:**

```csharp
// Interface
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
    Task SaveChangesAsync();
}

// Generic Base Implementation
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly SapaFoRestRmsContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(SapaFoRestRmsContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }
    
    // Implementations...
}

// Specific Repository
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<bool> IsEmailExistsAsync(string email);
}
```

**Lợi ích:**
- ✅ Tách biệt data access logic
- ✅ Dễ dàng test với mock repositories
- ✅ Có thể thay đổi data source mà không ảnh hưởng business logic
- ✅ Code reuse thông qua Generic Repository

---

### 3.2. UNIT OF WORK PATTERN ⭐⭐⭐

**Mục đích:** Quản lý transactions và đảm bảo consistency khi thực hiện nhiều operations cùng lúc.

**Cấu trúc:**

```
IUnitOfWork (Interface)
    ↓
UnitOfWork (Implementation)
    ├── IUserRepository Users
    ├── IRoleRepository Roles
    ├── IPositionRepository Positions
    └── Transaction Management
```

**Vị trí trong code:**
- `DataAccessLayer/UnitOfWork/Interfaces/IUnitOfWork.cs`
- `DataAccessLayer/UnitOfWork/UnitOfWork.cs`

**Ví dụ:**

```csharp
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IStaffProfileRepository StaffProfiles { get; }
    IPositionRepository Positions { get; }
    
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task<int> SaveChangesAsync();
    Task CommitAsync();
    Task RollbackAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly SapaFoRestRmsContext _context;
    private IUserRepository _users;
    
    public IUserRepository Users => _users ??= new UserRepository(_context);
    
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
```

**Lợi ích:**
- ✅ Quản lý transaction tập trung
- ✅ Đảm bảo ACID properties
- ✅ Tránh việc save changes nhiều lần
- ✅ Dễ dàng rollback khi có lỗi

**Sử dụng trong Service:**

```csharp
public class UserService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task CreateUserAsync(User user)
    {
        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }
}
```

---

### 3.3. DEPENDENCY INJECTION PATTERN ⭐⭐⭐

**Mục đích:** Inversion of Control (IoC) - Giảm coupling giữa các components, dễ dàng test và maintain.

**Cấu trúc:**

```
Program.cs (DI Container)
    ├── Register Services
    ├── Register Repositories
    ├── Register UnitOfWork
    └── Register AutoMapper
```

**Vị trí trong code:**
- `SapaFoRestRMSAPI/Program.cs` (lines 95-208)

**Ví dụ:**

```csharp
// Registration trong Program.cs
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();

// Sử dụng trong Controller
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    
    public UsersController(IUserService userService)
    {
        _userService = userService; // Injected by DI container
    }
}
```

**Lợi ích:**
- ✅ Loose coupling giữa components
- ✅ Dễ dàng test với mock objects
- ✅ Centralized configuration
- ✅ Lifecycle management (Scoped, Singleton, Transient)

**Lifecycle types được sử dụng:**
- `AddScoped`: Repositories, Services, UnitOfWork (1 instance per HTTP request)
- `AddSingleton`: CloudinaryService (1 instance cho toàn bộ app)

---

### 3.4. SERVICE LAYER PATTERN ⭐⭐⭐

**Mục đích:** Tách biệt business logic khỏi Controllers và Data Access layer.

**Cấu trúc:**

```
Controller → Service Interface → Service Implementation → Repository
```

**Vị trí trong code:**
- `BusinessAccessLayer/Services/Interfaces/` (30+ interfaces)
- `BusinessAccessLayer/Services/` (27+ services)

**Ví dụ:**

```csharp
// Interface
public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllAsync(CancellationToken ct = default);
    Task<UserDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<UserDto> CreateAsync(UserCreateRequest request, CancellationToken ct = default);
}

// Implementation
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IRoleRepository _roleRepository;
    
    public UserService(IUnitOfWork unitOfWork, IMapper mapper, IRoleRepository roleRepository)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _roleRepository = roleRepository;
    }
    
    public async Task<IEnumerable<UserDto>> GetAllAsync(CancellationToken ct = default)
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        // Business logic here
        return _mapper.Map<IEnumerable<UserDto>>(users);
    }
}
```

**Lợi ích:**
- ✅ Tách biệt business logic
- ✅ Reusable logic
- ✅ Dễ dàng test
- ✅ Single Responsibility Principle

---

### 3.5. DTO PATTERN (DATA TRANSFER OBJECT) ⭐⭐

**Mục đích:** Truyền dữ liệu giữa các layers mà không expose domain models trực tiếp.

**Cấu trúc:**

```
Domain Models (Internal)
    ↓
DTOs (External)
    ├── Request DTOs (Create, Update)
    ├── Response DTOs (List, Detail)
    └── Search DTOs
```

**Vị trí trong code:**
- `BusinessAccessLayer/DTOs/` (50+ DTOs)

**Ví dụ:**

```csharp
// Domain Model (Internal)
public class User
{
    public int UserId { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; } // Sensitive
}

// DTO (External)
public class UserDto
{
    public int UserId { get; set; }
    public string Email { get; set; }
    public string RoleName { get; set; }
    // No PasswordHash exposed
}

public class UserCreateRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
    public int RoleId { get; set; }
}
```

**Lợi ích:**
- ✅ Bảo mật (không expose sensitive data)
- ✅ Versioning API dễ dàng
- ✅ Tách biệt internal models và external contracts
- ✅ Validation ở DTO level

---

### 3.6. GENERIC REPOSITORY PATTERN ⭐⭐

**Mục đích:** Tái sử dụng code cho các CRUD operations cơ bản.

**Cấu trúc:**

```
IRepository<T> (Generic Interface)
    ↓
Repository<T> (Generic Implementation)
    ↓
Specific Repositories extend với custom methods
```

**Ví dụ:**

```csharp
// Generic Repository
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly SapaFoRestRmsContext _context;
    protected readonly DbSet<T> _dbSet;
    
    // Common CRUD operations
    public async Task<T?> GetByIdAsync(int id) { ... }
    public async Task<IEnumerable<T>> GetAllAsync() { ... }
    public async Task AddAsync(T entity) { ... }
}

// Specific Repository extends Generic
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(SapaFoRestRmsContext context) : base(context) { }
    
    // Custom methods
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
    }
}
```

**Lợi ích:**
- ✅ Code reuse
- ✅ Consistent API across repositories
- ✅ Giảm boilerplate code

---

### 3.7. MAPPER PATTERN (AUTOMAPPER) ⭐⭐

**Mục đích:** Tự động map giữa Domain Models và DTOs.

**Cấu trúc:**

```
AutoMapper Configuration
    ↓
Mapping Profiles
    ├── AutoMapperProfile
    ├── MappingProfile
    └── MarketingCampaignMappingProfile
```

**Vị trí trong code:**
- `BusinessAccessLayer/Mapping/AutoMapperProfile.cs`
- `BusinessAccessLayer/Mapping/MappingProfile.cs`

**Ví dụ:**

```csharp
// Configuration trong Program.cs
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Mapping Profile
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>();
        CreateMap<UserCreateRequest, User>();
        CreateMap<UserUpdateRequest, User>();
        CreateMap<Role, RoleDto>();
    }
}

// Sử dụng trong Service
public class UserService
{
    private readonly IMapper _mapper;
    
    public async Task<UserDto> GetByIdAsync(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        return _mapper.Map<UserDto>(user); // Auto mapping
    }
}
```

**Lợi ích:**
- ✅ Giảm manual mapping code
- ✅ Type-safe mapping
- ✅ Centralized mapping configuration
- ✅ Dễ maintain

---

### 3.8. STRATEGY PATTERN (IMPLICIT) ⭐

**Mục đích:** Cho phép chọn algorithm/behavior tại runtime.

**Ví dụ trong project:**

```csharp
// Different authentication strategies
public interface IAuthService { }
public class AuthService : IAuthService { } // Email/Password
public class ExternalAuthService : IExternalAuthService { } // OAuth
public class PhoneAuthService : IPhoneAuthService { } // OTP

// Different payment strategies (có thể implement sau)
public interface IPaymentService { }
public class CashPaymentService : IPaymentService { }
public class CardPaymentService : IPaymentService { }
```

---

### 3.9. FACTORY PATTERN (IMPLICIT - DI Container) ⭐

**Mục đích:** Tạo objects mà không cần specify exact class.

**Ví dụ:**

```csharp
// DI Container acts as Factory
builder.Services.AddScoped<IUserService, UserService>();
// Khi inject IUserService, DI container tự động tạo UserService instance
```

---

## 4. SƠ ĐỒ KIẾN TRÚC

### 4.1. Request Flow

```
HTTP Request
    ↓
Controller (Presentation Layer)
    ├── Validate Request
    ├── Call Service
    └── Return Response
    ↓
Service (Business Logic Layer)
    ├── Business Rules
    ├── Validation
    ├── DTO Mapping
    └── Call Repository/UnitOfWork
    ↓
Repository (Data Access Layer)
    ├── Database Queries
    └── Entity Operations
    ↓
UnitOfWork
    ├── Transaction Management
    └── Save Changes
    ↓
DbContext (Entity Framework)
    ↓
Database
```

### 4.2. Dependency Graph

```
Controller
    ↓ depends on
Service Interface
    ↓ implemented by
Service Implementation
    ↓ depends on
    ├── UnitOfWork
    ├── Repository Interfaces
    ├── AutoMapper
    └── Other Services
        ↓
Repository Implementation
    ↓ depends on
DbContext
```

---

## 5. VÍ DỤ CODE HOÀN CHỈNH

### 5.1. Complete Flow Example

```csharp
// 1. Controller
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    
    public UsersController(IUserService userService)
    {
        _userService = userService; // DI
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users);
    }
}

// 2. Service
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IRoleRepository _roleRepository;
    
    public UserService(IUnitOfWork unitOfWork, IMapper mapper, IRoleRepository roleRepository)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _roleRepository = roleRepository;
    }
    
    public async Task<IEnumerable<UserDto>> GetAllAsync(CancellationToken ct = default)
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        var activeUsers = users.Where(u => !u.IsDeleted).ToList();
        
        var userDtos = new List<UserDto>();
        foreach (var user in activeUsers)
        {
            var userDto = _mapper.Map<UserDto>(user);
            var role = await _roleRepository.GetByIdAsync(user.RoleId);
            userDto.RoleName = role?.RoleName ?? "Unknown";
            userDtos.Add(userDto);
        }
        
        return userDtos;
    }
}

// 3. Repository
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(SapaFoRestRmsContext context) : base(context) { }
    
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
    }
}

// 4. UnitOfWork
public class UnitOfWork : IUnitOfWork
{
    private readonly SapaFoRestRmsContext _context;
    private IUserRepository _users;
    
    public IUserRepository Users => _users ??= new UserRepository(_context);
    
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
```

---

## 6. BEST PRACTICES ĐƯỢC ÁP DỤNG

### ✅ **Separation of Concerns**
- Controllers chỉ handle HTTP requests/responses
- Business logic ở Service layer
- Data access ở Repository layer

### ✅ **Dependency Inversion Principle**
- Depend on abstractions (interfaces), not concretions
- Tất cả dependencies đều inject qua constructor

### ✅ **Single Responsibility Principle**
- Mỗi class có 1 trách nhiệm duy nhất
- Repository: Data access
- Service: Business logic
- Controller: HTTP handling

### ✅ **Open/Closed Principle**
- Extend functionality thông qua interfaces
- Không modify existing code khi thêm features

---

## 7. SO SÁNH VỚI CÁC PATTERNS KHÁC

### Repository Pattern vs Active Record
- ✅ **Repository**: Tách biệt data access (như project này)
- ❌ **Active Record**: Model tự quản lý data access (không dùng)

### Unit of Work vs Transaction Script
- ✅ **Unit of Work**: Quản lý transaction tập trung (đang dùng)
- ❌ **Transaction Script**: Logic trong stored procedures (không dùng)

---

## 8. KẾT LUẬN

Project **SapaFoRestRMS API** sử dụng một kiến trúc **clean và maintainable** với:

- ✅ **7+ Design Patterns** được áp dụng đúng cách
- ✅ **Layered Architecture** rõ ràng
- ✅ **Dependency Injection** toàn diện
- ✅ **Separation of Concerns** tốt
- ✅ **Testable** architecture

**Điểm mạnh:**
- Code structure rõ ràng, dễ maintain
- Dễ dàng test với mock objects
- Scalable và extensible

**Có thể cải thiện:**
- Thêm CQRS pattern cho complex queries
- Thêm Mediator pattern để giảm coupling
- Thêm Specification pattern cho complex queries

---

**Tài liệu này được tạo tự động dựa trên phân tích codebase.**  
**Cập nhật lần cuối:** 2025-01-15

