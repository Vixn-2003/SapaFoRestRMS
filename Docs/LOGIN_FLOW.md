# 🔐 Login Flow - SapaFoRest RMS

## 📋 Tổng quan

Login flow trong ứng dụng sử dụng **Cookie Authentication** kết hợp với **JWT Token** và **Session Storage** để quản lý authentication state.

---

## 🔄 Flow Diagram

```
┌─────────────┐
│   Browser   │
└──────┬──────┘
       │
       │ 1. GET /Auth/Login
       ▼
┌─────────────────┐
│ AuthController  │
│  Login (GET)     │
└──────┬──────────┘
       │
       │ 2. Check if already authenticated
       │    - If YES → Redirect by Role
       │    - If NO  → Show Login View
       ▼
┌─────────────────┐
│  Login.cshtml   │
│  (Login Form)   │
└──────┬──────────┘
       │
       │ 3. User submits Email + Password
       │    POST /Auth/Login
       ▼
┌─────────────────┐
│ AuthController  │
│  Login (POST)   │
└──────┬──────────┘
       │
       │ 4. Validate ModelState
       │    - If invalid → Return View with errors
       │    - If valid   → Continue
       ▼
┌─────────────────┐
│   ApiService    │
│  LoginAsync()   │
└──────┬──────────┘
       │
       │ 5. Serialize LoginRequest to JSON
       │    POST {BaseUrl}/Auth/login
       ▼
┌─────────────────┐
│  Backend API    │
│  AuthController │
│  /Auth/login    │
└──────┬──────────┘
       │
       │ 6. Validate credentials
       │    - Check Email & Password
       │    - Generate JWT Token
       │    - Generate Refresh Token
       │    - Return LoginResponse
       ▼
┌─────────────────┐
│   ApiService    │
│  LoginAsync()   │
└──────┬──────────┘
       │
       │ 7. Deserialize LoginResponse
       │    - Save Token to Session
       │    - Save RefreshToken to Session
       │    - Return LoginResponse
       ▼
┌─────────────────┐
│ AuthController  │
│  Login (POST)   │
└──────┬──────────┘
       │
       │ 8. Create Claims Identity
       │    - NameIdentifier (UserId)
       │    - Name (FullName)
       │    - Email
       │    - Role
       │    - Token (JWT)
       ▼
┌─────────────────┐
│  SignInAsync()  │
│  (Cookie Auth)  │
└──────┬──────────┘
       │
       │ 9. Create Authentication Cookie
       │    - Set ClaimsPrincipal
       │    - Set ExpiresUtc (1 hour)
       │    - Set IsPersistent = true
       ▼
┌─────────────────┐
│   Redirect      │
│  (By Role)      │
└──────┬──────────┘
       │
       │ 10. Redirect to appropriate page
       │     - Owner/Admin → /Admin
       │     - Manager     → /Users
       │     - Staff       → /Home
       │     - Customer    → /Home
       ▼
┌─────────────┐
│   Browser    │
│ (Authenticated)
└─────────────┘
```

---

## 📝 Chi tiết từng bước

### **Bước 1: User truy cập Login Page**

**File:** `AuthController.cs` - Method `Login (GET)`

```csharp
[HttpGet]
public IActionResult Login(string returnUrl = null)
{
    // Kiểm tra nếu đã đăng nhập
    if (User?.Identity?.IsAuthenticated == true)
    {
        // Redirect theo Role
        if (User.IsInRole("Owner")) return RedirectToAction("Index", "Admin");
        if (User.IsInRole("Admin")) return RedirectToAction("Index", "Admin");
        // ... các role khác
    }
    
    // Nếu chưa đăng nhập → hiển thị form
    return View(new LoginRequest());
}
```

**Kết quả:** Hiển thị `Login.cshtml` với form đăng nhập.

---

### **Bước 2: User submit form**

**File:** `Login.cshtml`

Form gửi POST request với:
- `Email` (required, email format)
- `Password` (required)
- Anti-forgery token (`@Html.AntiForgeryToken()`)

---

### **Bước 3: Controller nhận request**

**File:** `AuthController.cs` - Method `Login (POST)`

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Login(LoginRequest model, string returnUrl = null)
{
    // 1. Validate ModelState
    if (!ModelState.IsValid)
    {
        return View(model); // Return với errors
    }
    
    // 2. Gọi API Service để authenticate
    var authResponse = await _apiService.LoginAsync(model);
    
    // 3. Xử lý response...
}
```

---

### **Bước 4: API Service gọi Backend**

**File:** `AuthApiService.cs` - Method `LoginAsync()`

```csharp
public async Task<LoginResponse?> LoginAsync(LoginRequest request)
{
    // 1. Serialize request to JSON
    var json = JsonSerializer.Serialize(request);
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    
    // 2. POST to Backend API
    var response = await _httpClient.PostAsync(
        $"{GetApiBaseUrl()}/Auth/login", 
        content
    );
    
    // 3. Deserialize response
    if (response.IsSuccessStatusCode)
    {
        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(...);
        
        // 4. Lưu Token vào Session
        SetToken(loginResponse.Token);
        
        // 5. Lưu RefreshToken vào Session
        if (!string.IsNullOrEmpty(loginResponse.RefreshToken))
        {
            _httpContextAccessor.HttpContext?.Session
                .SetString("RefreshToken", loginResponse.RefreshToken);
        }
        
        return loginResponse;
    }
    
    return null; // Login failed
}
```

**Endpoint Backend:** `POST /Auth/login`

**Request Body:**
```json
{
  "Email": "user@example.com",
  "Password": "password123"
}
```

**Response Body (Success):**
```json
{
  "UserId": 1,
  "FullName": "Nguyễn Văn A",
  "Email": "user@example.com",
  "RoleId": 2,
  "RoleName": "Admin",
  "Token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "RefreshToken": "refresh_token_string..."
}
```

---

### **Bước 5: Tạo Claims Identity**

**File:** `AuthController.cs` - Method `Login (POST)`

```csharp
if (authResponse != null)
{
    // Tạo Claims từ LoginResponse
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, authResponse.UserId.ToString()), // User ID
        new Claim(ClaimTypes.Name, authResponse.FullName ?? ""),              // Full Name
        new Claim(ClaimTypes.Email, authResponse.Email ?? ""),                 // Email
        new Claim(ClaimTypes.Role, GetRoleName(authResponse.RoleId)),          // Role
        new Claim("Token", authResponse.Token ?? "")                           // JWT Token
    };
    
    // Tạo ClaimsIdentity
    var claimsIdentity = new ClaimsIdentity(
        claims, 
        CookieAuthenticationDefaults.AuthenticationScheme
    );
    
    // Tạo AuthenticationProperties
    var authProperties = new AuthenticationProperties
    {
        IsPersistent = true,                                    // Cookie persistent
        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)         // Expire sau 1 giờ
    };
    
    // Sign in với Cookie Authentication
    await HttpContext.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(claimsIdentity),
        authProperties
    );
}
```

**Claims được lưu:**
- `ClaimTypes.NameIdentifier` → User ID (dùng để lấy user ID sau này)
- `ClaimTypes.Name` → Full Name
- `ClaimTypes.Email` → Email
- `ClaimTypes.Role` → Role Name (Owner, Admin, Manager, Staff, Customer)
- `"Token"` → JWT Token (dùng cho API calls)

---

### **Bước 6: Redirect theo Role**

**File:** `AuthController.cs` - Method `Login (POST)`

```csharp
var redirectUrl = authResponse.RoleId switch
{
    1 => returnUrl ?? Url.Action("Index", "Admin"),  // Owner
    2 => returnUrl ?? Url.Action("Index", "Admin"),  // Admin
    3 => returnUrl ?? Url.Action("Index", "Users"),   // Manager
    4 => returnUrl ?? Url.Action("Index", "Home"),     // Staff
    5 => returnUrl ?? Url.Action("Index", "Home"),     // Customer
    _ => returnUrl ?? Url.Action("Index", "Home")
};

return LocalRedirect(redirectUrl);
```

**Role Mapping:**
- `RoleId = 1` → **Owner** → `/Admin`
- `RoleId = 2` → **Admin** → `/Admin`
- `RoleId = 3` → **Manager** → `/Users`
- `RoleId = 4` → **Staff** → `/Home`
- `RoleId = 5` → **Customer** → `/Home`

---

## 🔑 Token Storage Strategy

### **1. JWT Token (Access Token)**

**Lưu ở 2 nơi:**
- ✅ **Session Storage:** `Session["Token"]` (dùng cho backward compatibility)
- ✅ **Claims:** `User.FindFirst("Token")?.Value` (dùng cho API calls)

**Cách lấy Token:**
```csharp
// Từ BaseApiService
public string? GetToken()
{
    // 1. Thử lấy từ Session
    var tokenFromSession = httpContext.Session.GetString("Token");
    if (!string.IsNullOrEmpty(tokenFromSession))
        return tokenFromSession;
    
    // 2. Thử lấy từ Claims
    var tokenFromClaims = httpContext.User?.FindFirst("Token")?.Value;
    return tokenFromClaims;
}
```

### **2. Refresh Token**

**Lưu ở:**
- ✅ **Session Storage:** `Session["RefreshToken"]`

**Dùng để:**
- Refresh Access Token khi hết hạn (401 Unauthorized)
- Method `TryRefreshTokenAsync()` trong `BaseApiService`

---

## 🍪 Cookie Authentication

### **Cookie Properties:**
- **Name:** `SapaFoRestRMS.Auth` (config trong `Program.cs`)
- **Expires:** 1 hour (config trong `AuthController`)
- **IsPersistent:** `true` (cookie sẽ persist sau khi đóng browser)
- **Scheme:** `CookieAuthenticationDefaults.AuthenticationScheme`

### **Cookie chứa:**
- Claims Identity (UserId, Name, Email, Role, Token)
- Authentication metadata

---

## 🔄 Auto Token Refresh

Khi gọi API và nhận **401 Unauthorized**, hệ thống tự động refresh token:

**File:** `BaseApiService.cs` - Method `SendWithAutoRefreshAsync()`

```csharp
public async Task<HttpResponseMessage> SendWithAutoRefreshAsync(...)
{
    var response = await send(client);
    
    // Nếu nhận 401 Unauthorized
    if (response.StatusCode == HttpStatusCode.Unauthorized)
    {
        // Thử refresh token
        if (await TryRefreshTokenAsync())
        {
            // Retry request với token mới
            response = await send(client);
        }
    }
    
    return response;
}
```

**Flow Refresh Token:**
1. Gọi API → Nhận 401
2. Lấy RefreshToken từ Session
3. POST `/Auth/refresh-token` với RefreshToken
4. Nhận Token mới
5. Lưu Token mới vào Session và Claims
6. Retry request ban đầu với Token mới

---

## 🚪 Logout Flow

**File:** `AuthController.cs` - Method `Logout()`

```csharp
[HttpPost]
public async Task<IActionResult> Logout()
{
    // 1. Sign out Cookie Authentication
    await HttpContext.SignOutAsync(
        CookieAuthenticationDefaults.AuthenticationScheme
    );
    
    // 2. Clear tokens từ Session
    _apiService.Logout(); // Xóa Token và RefreshToken
    
    // 3. Redirect về Login
    return RedirectToAction("Login");
}
```

**Method `Logout()` trong ApiService:**
```csharp
public void Logout()
{
    ClearToken(); // Xóa Session["Token"] và Session["RefreshToken"]
}
```

---

## 📊 Data Flow Summary

```
┌─────────────────────────────────────────────────────────┐
│                    STORAGE LOCATIONS                     │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  1. COOKIE (Authentication Cookie)                      │
│     - Claims: UserId, Name, Email, Role, Token         │
│     - Expires: 1 hour                                    │
│     - Used for: Authorization checks, User.Identity     │
│                                                          │
│  2. SESSION                                              │
│     - Session["Token"] → JWT Access Token               │
│     - Session["RefreshToken"] → Refresh Token           │
│     - Used for: API calls, Token refresh                │
│                                                          │
│  3. CLAIMS (in Cookie)                                   │
│     - ClaimTypes.NameIdentifier → User ID                │
│     - ClaimTypes.Name → Full Name                       │
│     - ClaimTypes.Email → Email                           │
│     - ClaimTypes.Role → Role Name                       │
│     - "Token" → JWT Token                               │
│     - Used for: Quick access to user info                │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

---

## 🛡️ Security Features

1. **Anti-Forgery Token:** Bảo vệ CSRF attacks
2. **JWT Token:** Stateless authentication cho API calls
3. **Refresh Token:** Tự động refresh khi token hết hạn
4. **Cookie HttpOnly:** Cookie chỉ accessible từ server (default trong ASP.NET Core)
5. **Role-based Authorization:** Kiểm tra quyền truy cập theo Role

---

## 📌 Key Points

✅ **Dual Storage:** Token được lưu ở cả Session và Claims để đảm bảo backward compatibility

✅ **Auto Refresh:** Tự động refresh token khi gặp 401 Unauthorized

✅ **Role-based Redirect:** Redirect user đến đúng trang theo Role sau khi login

✅ **Persistent Cookie:** Cookie persist sau khi đóng browser (IsPersistent = true)

✅ **1 Hour Expiry:** Cookie và token expire sau 1 giờ

---

## 🔍 Debugging Tips

### **Kiểm tra User đã đăng nhập:**
```csharp
if (User?.Identity?.IsAuthenticated == true)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var email = User.FindFirst(ClaimTypes.Email)?.Value;
    var role = User.FindFirst(ClaimTypes.Role)?.Value;
}
```

### **Kiểm tra Token trong Session:**
```csharp
var token = HttpContext.Session.GetString("Token");
var refreshToken = HttpContext.Session.GetString("RefreshToken");
```

### **Kiểm tra Claims:**
```csharp
var claims = User.Claims.ToList();
foreach (var claim in claims)
{
    Console.WriteLine($"{claim.Type}: {claim.Value}");
}
```

---

## 📚 Related Files

- **Controller:** `Controllers/AuthController.cs`
- **Service:** `Services/Api/AuthApiService.cs`
- **Base Service:** `Services/Api/BaseApiService.cs`
- **DTOs:** 
  - `DTOs/Auth/LoginRequest.cs`
  - `DTOs/Auth/LoginResponse.cs`
- **View:** `Views/Auth/Login.cshtml`
- **Config:** `Program.cs` (Cookie Authentication setup)

---

**Last Updated:** 2024-11-12

