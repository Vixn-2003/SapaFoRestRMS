# 🧪 Tests Project - SapaFoRestRMS

Project này chứa các Unit Tests cho SapaFoRestRMS API sử dụng **xUnit**, **Moq**, và **FluentAssertions**.

## 📋 Cấu trúc Project

```
Tests/
├── Services/
│   └── UserServiceTests.cs      # Unit tests cho UserService
├── Tests.csproj                  # Project configuration
└── README.md                     # Tài liệu này
```

## 🛠️ Công nghệ sử dụng

- **xUnit 2.6.2**: Testing framework
- **Moq 4.20.70**: Mocking framework
- **FluentAssertions 6.12.0**: Assertion library
- **AutoMapper 13.0.1**: Mapping library (để test mapping)
- **Coverlet 6.0.0**: Code coverage collector

## 🚀 Cách chạy Tests

### Chạy tất cả tests:
```bash
dotnet test Backend/Tests/Tests.csproj
```

### Chạy với verbose output:
```bash
dotnet test Backend/Tests/Tests.csproj --verbosity normal
```

### Chạy test cụ thể:
```bash
dotnet test Backend/Tests/Tests.csproj --filter "GetAllAsync_ReturnsActiveUsers"
```

### Chạy tests với code coverage:
```bash
dotnet test Backend/Tests/Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Chạy tests trong Visual Studio:
1. Mở Test Explorer (Test → Test Explorer)
2. Build solution (Ctrl+Shift+B)
3. Click "Run All" hoặc chọn test cụ thể để chạy

## 📝 Test Cases hiện có

### UserServiceTests
- ✅ `GetAllAsync_ReturnsActiveUsers`: Test lọc users không bị xóa
- ✅ `GetByIdAsync_ReturnsCorrectUser`: Test lấy user theo ID
- ✅ `CreateAsync_AddsNewUser`: Test tạo user mới
- ✅ `UpdateAsync_UpdatesUserSuccessfully`: Test cập nhật user
- ✅ `DeleteAsync_SoftDeletesUser`: Test soft delete user

## 🎯 Nguyên tắc Testing

1. **Isolation**: Mỗi test độc lập, không phụ thuộc vào test khác
2. **Mocking**: Tất cả dependencies được mock, không kết nối DB thật
3. **AAA Pattern**: Arrange → Act → Assert
4. **Test Data**: Sử dụng in-memory test data
5. **Coverage**: Test cả happy path và edge cases

## 📦 Dependencies

Project này reference:
- `BusinessAccessLayer`: Để test các Services
- `DomainAccessLayer`: Để sử dụng Domain Models

## 🔧 Build Project

```bash
cd Backend/Tests
dotnet build
```

## 📊 Code Coverage

Để xem code coverage report:
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

Sau đó mở file `coverage.cobertura.xml` bằng tool như ReportGenerator.

## 📚 Tài liệu tham khảo

- [xUnit Documentation](https://xunit.net/)
- [Moq Quickstart](https://github.com/moq/moq4/wiki/Quickstart)
- [FluentAssertions Documentation](https://fluentassertions.com/)

---

**Lưu ý**: Tất cả tests đều sử dụng mocking, không cần database connection.

