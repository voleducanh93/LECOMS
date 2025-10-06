# ====== Build stage ======
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# (A) Dễ dùng: copy toàn bộ source rồi restore
COPY ["LECOMS.API/LECOMS.API.csproj", "LECOMS.API/"]
COPY ["LECOMS.Common/LECOMS.Common.csproj", "LECOMS.Common/"]
COPY ["LECOMS.Data/LECOMS.Data.csproj", "LECOMS.Data/"]
COPY ["LECOMS.Repository/LECOMS.Repository.csproj", "LECOMS.Repository/"]
COPY ["LECOMS.RepositoryContract/LECOMS.RepositoryContract.csproj", "LECOMS.RepositoryContract/"]
COPY ["LECOMS.Service/LECOMS.Service.csproj", "LECOMS.Service/"]
COPY ["LECOMS.ServiceContract/LECOMS.ServiceContract.csproj", "LECOMS.ServiceContract/"]

RUN dotnet restore "LECOMS.API/LECOMS.API.csproj"

COPY . .

# Publish tất cả project dạng web/api (hoặc chỉ 1 project chính nếu bạn muốn)
# Lưu ý: /p:UseAppHost=false giúp image nhỏ hơn (không kèm native host)
RUN dotnet publish "LECOMS.API/LECOMS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ====== Runtime stage ======
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Mặc định lắng nghe cổng 8080 trong container
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Development

COPY --from=build /app/publish ./

EXPOSE 8080

# Tránh phải ghi đúng tên DLL: chạy DLL đầu tiên tìm thấy
ENTRYPOINT ["dotnet", "LECOMS.API.dll"]
