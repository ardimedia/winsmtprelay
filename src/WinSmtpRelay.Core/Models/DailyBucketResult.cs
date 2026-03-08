namespace WinSmtpRelay.Core.Models;

public record DailyBucketResult(string Date, int Sent, int Failed, int Bounced);
