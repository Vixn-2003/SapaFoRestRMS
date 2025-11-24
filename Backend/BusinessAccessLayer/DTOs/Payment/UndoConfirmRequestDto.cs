namespace BusinessAccessLayer.DTOs.Payment;

public class UndoConfirmRequestDto
{
    public int StaffId { get; set; }

    public string Reason { get; set; } = string.Empty;
}

