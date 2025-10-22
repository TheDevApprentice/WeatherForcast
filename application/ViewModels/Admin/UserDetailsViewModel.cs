namespace application.ViewModels.Admin
{
    public class UserDetailsViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<ClaimViewModel> Claims { get; set; } = new();
        public List<ApiKeyViewModel> ApiKeys { get; set; } = new();
        public List<SessionViewModel> Sessions { get; set; } = new();
        
        public string FullName => $"{FirstName} {LastName}";
        public int ActiveSessionsCount => Sessions.Count(s => !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow);
        public int ActiveApiKeysCount => ApiKeys.Count(k => k.IsActive && !k.IsRevoked);
    }

    public class ClaimViewModel
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class ApiKeyViewModel
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Scopes { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public long RequestCount { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;
        public string Status => IsRevoked ? "Révoquée" : IsExpired ? "Expirée" : IsActive ? "Active" : "Inactive";
    }

    public class SessionViewModel
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public bool IsExpired => ExpiresAt < DateTime.UtcNow;
        public bool IsActive => !IsRevoked && !IsExpired;
        public string Status => IsRevoked ? "Révoquée" : IsExpired ? "Expirée" : "Active";
    }
}
