namespace application.ViewModels.Admin
{
    public class EditRolesViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> AvailableRoles { get; set; } = new();
        public List<string> SelectedRoles { get; set; } = new();
        public List<ClaimViewModel> Claims { get; set; } = new();
    }
}
