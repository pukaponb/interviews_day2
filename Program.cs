using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace _227;

public class Program
{
    private static int _passedCount = 0;
    private static int _failedCount = 0;

    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        PrintHeader();

        var engine = new LoanPaymentEngine();
        var sw = Stopwatch.StartNew();

        // -------------------------------------------------------------
        // Test Suite Execution
        // -------------------------------------------------------------
        RunTest("Test 1: Example 1 from Specification (Standard Multi-Tier Waterfall)", () =>
        {
            var installments = new List<Installment>
            {
                new(1, new DateTime(2026, 1, 1), 500m, 300m, 200m, 5000m),
                new(2, new DateTime(2026, 2, 1), 0m, 0m, 200m, 5000m)
            };
            decimal payment = 1500m;

            var result = engine.AllocatePayment(installments, payment);

            AssertEqual(500m, result.TotalPenaltyPaid, "TotalPenaltyPaid");
            AssertEqual(300m, result.TotalOverdueInterestPaid, "TotalOverdueInterestPaid");
            AssertEqual(200m, result.TotalCurrentInterestPaid, "TotalCurrentInterestPaid");
            AssertEqual(500m, result.TotalPrincipalPaid, "TotalPrincipalPaid");
            AssertEqual(0m, result.OverpaymentAdvance, "OverpaymentAdvance");

            AssertEqual(2, result.RemainingInstallments.Count, "RemainingInstallments Count");

            // Installment 1 Remaining
            var rem1 = result.RemainingInstallments[0];
            AssertEqual(1, rem1.Id, "Inst1 Id");
            AssertEqual(0m, rem1.Penalty, "Inst1 Remaining Penalty");
            AssertEqual(0m, rem1.OverdueInterest, "Inst1 Remaining OverdueInterest");
            AssertEqual(0m, rem1.CurrentInterest, "Inst1 Remaining CurrentInterest");
            AssertEqual(4500m, rem1.Principal, "Inst1 Remaining Principal");

            // Installment 2 Remaining
            var rem2 = result.RemainingInstallments[1];
            AssertEqual(2, rem2.Id, "Inst2 Id");
            AssertEqual(0m, rem2.Penalty, "Inst2 Remaining Penalty");
            AssertEqual(0m, rem2.OverdueInterest, "Inst2 Remaining OverdueInterest");
            AssertEqual(200m, rem2.CurrentInterest, "Inst2 Remaining CurrentInterest");
            AssertEqual(5000m, rem2.Principal, "Inst2 Remaining Principal");

            PrintAllocationSummary(result, payment);
        });

        RunTest("Test 2: Exact Full Payoff (Zero Remaining Debt, Zero Advance)", () =>
        {
            var installments = new List<Installment>
            {
                new(1, new DateTime(2026, 1, 1), 500m, 300m, 200m, 5000m), // Total: 6000
                new(2, new DateTime(2026, 2, 1), 0m, 0m, 200m, 5000m)       // Total: 5200
            };
            decimal payment = 11200m; // Total debt = 6000 + 5200 = 11200

            var result = engine.AllocatePayment(installments, payment);

            AssertEqual(500m, result.TotalPenaltyPaid, "TotalPenaltyPaid");
            AssertEqual(300m, result.TotalOverdueInterestPaid, "TotalOverdueInterestPaid");
            AssertEqual(400m, result.TotalCurrentInterestPaid, "TotalCurrentInterestPaid");
            AssertEqual(10000m, result.TotalPrincipalPaid, "TotalPrincipalPaid");
            AssertEqual(0m, result.OverpaymentAdvance, "OverpaymentAdvance");

            foreach (var rem in result.RemainingInstallments)
            {
                AssertEqual(0m, rem.Penalty, $"Inst {rem.Id} Remaining Penalty");
                AssertEqual(0m, rem.OverdueInterest, $"Inst {rem.Id} Remaining OverdueInterest");
                AssertEqual(0m, rem.CurrentInterest, $"Inst {rem.Id} Remaining CurrentInterest");
                AssertEqual(0m, rem.Principal, $"Inst {rem.Id} Remaining Principal");
            }
        });

        RunTest("Test 3: Overpayment / Advance Balance (Payment Exceeds Total Debt)", () =>
        {
            var installments = new List<Installment>
            {
                new(1, new DateTime(2026, 1, 1), 200m, 100m, 300m, 4000m), // Total: 4600
                new(2, new DateTime(2026, 2, 1), 0m, 0m, 300m, 4000m)       // Total: 4300
            };
            decimal payment = 15000m; // Total debt = 8900, Overpayment should be 6100

            var result = engine.AllocatePayment(installments, payment);

            AssertEqual(200m, result.TotalPenaltyPaid, "TotalPenaltyPaid");
            AssertEqual(100m, result.TotalOverdueInterestPaid, "TotalOverdueInterestPaid");
            AssertEqual(600m, result.TotalCurrentInterestPaid, "TotalCurrentInterestPaid");
            AssertEqual(8000m, result.TotalPrincipalPaid, "TotalPrincipalPaid");
            AssertEqual(6100m, result.OverpaymentAdvance, "OverpaymentAdvance");

            foreach (var rem in result.RemainingInstallments)
            {
                AssertEqual(0m, rem.Penalty, $"Inst {rem.Id} Remaining Penalty");
                AssertEqual(0m, rem.OverdueInterest, $"Inst {rem.Id} Remaining Overdue");
                AssertEqual(0m, rem.CurrentInterest, $"Inst {rem.Id} Remaining Current");
                AssertEqual(0m, rem.Principal, $"Inst {rem.Id} Remaining Principal");
            }
        });

        RunTest("Test 4: Partial Payment Exhausted at Step 1 (Penalty Fee)", () =>
        {
            var installments = new List<Installment>
            {
                new(1, new DateTime(2026, 1, 1), 500m, 300m, 200m, 5000m),
                new(2, new DateTime(2026, 2, 1), 100m, 50m, 200m, 5000m)
            };
            decimal payment = 300m; // Only covers part of Inst 1's 500 Penalty

            var result = engine.AllocatePayment(installments, payment);

            AssertEqual(300m, result.TotalPenaltyPaid, "TotalPenaltyPaid");
            AssertEqual(0m, result.TotalOverdueInterestPaid, "TotalOverdueInterestPaid");
            AssertEqual(0m, result.TotalCurrentInterestPaid, "TotalCurrentInterestPaid");
            AssertEqual(0m, result.TotalPrincipalPaid, "TotalPrincipalPaid");
            AssertEqual(0m, result.OverpaymentAdvance, "OverpaymentAdvance");

            var rem1 = result.RemainingInstallments[0];
            AssertEqual(200m, rem1.Penalty, "Inst1 Remaining Penalty (500-300)");
            AssertEqual(300m, rem1.OverdueInterest, "Inst1 Remaining Overdue");
            AssertEqual(200m, rem1.CurrentInterest, "Inst1 Remaining Current");
            AssertEqual(5000m, rem1.Principal, "Inst1 Remaining Principal");

            var rem2 = result.RemainingInstallments[1];
            AssertEqual(100m, rem2.Penalty, "Inst2 Remaining Penalty");
            AssertEqual(50m, rem2.OverdueInterest, "Inst2 Remaining Overdue");
            AssertEqual(200m, rem2.CurrentInterest, "Inst2 Remaining Current");
            AssertEqual(5000m, rem2.Principal, "Inst2 Remaining Principal");
        });

        RunTest("Test 5: Partial Payment Exhausted at Step 2 (Overdue Interest)", () =>
        {
            var installments = new List<Installment>
            {
                new(1, new DateTime(2026, 1, 1), 500m, 300m, 200m, 5000m)
            };
            decimal payment = 650m; // Penalty 500 + Overdue 150 (out of 300)

            var result = engine.AllocatePayment(installments, payment);

            AssertEqual(500m, result.TotalPenaltyPaid, "TotalPenaltyPaid");
            AssertEqual(150m, result.TotalOverdueInterestPaid, "TotalOverdueInterestPaid");
            AssertEqual(0m, result.TotalCurrentInterestPaid, "TotalCurrentInterestPaid");
            AssertEqual(0m, result.TotalPrincipalPaid, "TotalPrincipalPaid");
            AssertEqual(0m, result.OverpaymentAdvance, "OverpaymentAdvance");

            var rem1 = result.RemainingInstallments[0];
            AssertEqual(0m, rem1.Penalty, "Inst1 Remaining Penalty");
            AssertEqual(150m, rem1.OverdueInterest, "Inst1 Remaining Overdue");
            AssertEqual(200m, rem1.CurrentInterest, "Inst1 Remaining Current");
            AssertEqual(5000m, rem1.Principal, "Inst1 Remaining Principal");
        });

        RunTest("Test 6: Partial Payment Exhausted at Step 3 (Accrued / Current Interest)", () =>
        {
            var installments = new List<Installment>
            {
                new(1, new DateTime(2026, 1, 1), 500m, 300m, 200m, 5000m)
            };
            decimal payment = 900m; // Penalty 500 + Overdue 300 + Current 100 (out of 200)

            var result = engine.AllocatePayment(installments, payment);

            AssertEqual(500m, result.TotalPenaltyPaid, "TotalPenaltyPaid");
            AssertEqual(300m, result.TotalOverdueInterestPaid, "TotalOverdueInterestPaid");
            AssertEqual(100m, result.TotalCurrentInterestPaid, "TotalCurrentInterestPaid");
            AssertEqual(0m, result.TotalPrincipalPaid, "TotalPrincipalPaid");
            AssertEqual(0m, result.OverpaymentAdvance, "OverpaymentAdvance");

            var rem1 = result.RemainingInstallments[0];
            AssertEqual(0m, rem1.Penalty, "Inst1 Remaining Penalty");
            AssertEqual(0m, rem1.OverdueInterest, "Inst1 Remaining Overdue");
            AssertEqual(100m, rem1.CurrentInterest, "Inst1 Remaining Current");
            AssertEqual(5000m, rem1.Principal, "Inst1 Remaining Principal");
        });

        RunTest("Test 7: Zero Payment ($0.00)", () =>
        {
            var installments = new List<Installment>
            {
                new(1, new DateTime(2026, 1, 1), 500m, 300m, 200m, 5000m)
            };
            decimal payment = 0m;

            var result = engine.AllocatePayment(installments, payment);

            AssertEqual(0m, result.TotalPenaltyPaid, "TotalPenaltyPaid");
            AssertEqual(0m, result.TotalOverdueInterestPaid, "TotalOverdueInterestPaid");
            AssertEqual(0m, result.TotalCurrentInterestPaid, "TotalCurrentInterestPaid");
            AssertEqual(0m, result.TotalPrincipalPaid, "TotalPrincipalPaid");
            AssertEqual(0m, result.OverpaymentAdvance, "OverpaymentAdvance");

            var rem1 = result.RemainingInstallments[0];
            AssertEqual(500m, rem1.Penalty, "Inst1 Remaining Penalty");
            AssertEqual(300m, rem1.OverdueInterest, "Inst1 Remaining Overdue");
            AssertEqual(200m, rem1.CurrentInterest, "Inst1 Remaining Current");
            AssertEqual(5000m, rem1.Principal, "Inst1 Remaining Principal");
        });

        RunTest("Test 8: Chronological Out-of-Order Input Sorting (Oldest Due Date First)", () =>
        {
            // Input presented in mixed order: April, January, March, February
            var installments = new List<Installment>
            {
                new(4, new DateTime(2026, 4, 1), 0m, 0m, 100m, 1000m),
                new(1, new DateTime(2026, 1, 1), 100m, 100m, 100m, 1000m),
                new(3, new DateTime(2026, 3, 1), 0m, 0m, 100m, 1000m),
                new(2, new DateTime(2026, 2, 1), 50m, 50m, 100m, 1000m)
            };
            // Payment = 1300 (Pay full Inst 1: 100+100+100+1000 = 1300)
            decimal payment = 1300m;

            var result = engine.AllocatePayment(installments, payment);

            AssertEqual(100m, result.TotalPenaltyPaid, "TotalPenaltyPaid");
            AssertEqual(100m, result.TotalOverdueInterestPaid, "TotalOverdueInterestPaid");
            AssertEqual(100m, result.TotalCurrentInterestPaid, "TotalCurrentInterestPaid");
            AssertEqual(1000m, result.TotalPrincipalPaid, "TotalPrincipalPaid");
            AssertEqual(0m, result.OverpaymentAdvance, "OverpaymentAdvance");

            // Verify order in result is sorted by DueDate
            AssertEqual(1, result.RemainingInstallments[0].Id, "1st in result should be Jan (Id 1)");
            AssertEqual(2, result.RemainingInstallments[1].Id, "2nd in result should be Feb (Id 2)");
            AssertEqual(3, result.RemainingInstallments[2].Id, "3rd in result should be Mar (Id 3)");
            AssertEqual(4, result.RemainingInstallments[3].Id, "4th in result should be Apr (Id 4)");

            // Jan is completely paid off
            AssertEqual(0m, result.RemainingInstallments[0].Principal, "Inst 1 Principal Remaining");
            // Feb is completely unpaid
            AssertEqual(1000m, result.RemainingInstallments[1].Principal, "Inst 2 Principal Remaining");
        });

        RunTest("Test 9: High Decimal Precision (Satang / Fractional Cents)", () =>
        {
            var installments = new List<Installment>
            {
                new(1, new DateTime(2026, 1, 1), 123.45m, 67.89m, 210.50m, 1250.75m),
                new(2, new DateTime(2026, 2, 1), 0.00m, 0.00m, 210.50m, 1250.75m)
            };
            // Payment: 123.45 + 67.89 + 210.50 + 500.16 = 902.00
            decimal payment = 902.00m;

            var result = engine.AllocatePayment(installments, payment);

            AssertEqual(123.45m, result.TotalPenaltyPaid, "TotalPenaltyPaid");
            AssertEqual(67.89m, result.TotalOverdueInterestPaid, "TotalOverdueInterestPaid");
            AssertEqual(210.50m, result.TotalCurrentInterestPaid, "TotalCurrentInterestPaid");
            AssertEqual(500.16m, result.TotalPrincipalPaid, "TotalPrincipalPaid");
            AssertEqual(0.00m, result.OverpaymentAdvance, "OverpaymentAdvance");

            var rem1 = result.RemainingInstallments[0];
            AssertEqual(0.00m, rem1.Penalty, "Inst1 Remaining Penalty");
            AssertEqual(0.00m, rem1.OverdueInterest, "Inst1 Remaining Overdue");
            AssertEqual(0.00m, rem1.CurrentInterest, "Inst1 Remaining Current");
            AssertEqual(750.59m, rem1.Principal, "Inst1 Remaining Principal (1250.75 - 500.16)");
        });

        RunTest("Test 10: Empty Installments List (Advance Refund)", () =>
        {
            var installments = new List<Installment>();
            decimal payment = 5000m;

            var result = engine.AllocatePayment(installments, payment);

            AssertEqual(0m, result.TotalPenaltyPaid, "TotalPenaltyPaid");
            AssertEqual(0m, result.TotalOverdueInterestPaid, "TotalOverdueInterestPaid");
            AssertEqual(0m, result.TotalCurrentInterestPaid, "TotalCurrentInterestPaid");
            AssertEqual(0m, result.TotalPrincipalPaid, "TotalPrincipalPaid");
            AssertEqual(5000m, result.OverpaymentAdvance, "OverpaymentAdvance");
            AssertEqual(0, result.RemainingInstallments.Count, "RemainingInstallments Count");
        });

        RunTest("Test 11: Multi-Month Cascading Debt (5 Sequential Installments)", () =>
        {
            var installments = new List<Installment>
            {
                new(1, new DateTime(2026, 1, 1), 300m, 200m, 100m, 1000m), // Total = 1600
                new(2, new DateTime(2026, 2, 1), 200m, 150m, 100m, 1000m), // Total = 1450
                new(3, new DateTime(2026, 3, 1), 100m, 100m, 100m, 1000m), // Total = 1300
                new(4, new DateTime(2026, 4, 1), 0m, 0m, 100m, 1000m),     // Total = 1100
                new(5, new DateTime(2026, 5, 1), 0m, 0m, 100m, 1000m)      // Total = 1100
            };
            // Total debt = 1600 + 1450 + 1300 + 1100 + 1100 = 6550
            // Pay 4000:
            // Inst 1: 1600 paid (Remaining 0) -> Payment remaining: 2400
            // Inst 2: 1450 paid (Remaining 0) -> Payment remaining: 950
            // Inst 3: Pen 100, Overdue 100, Curr 100, Princ 650 (Remaining Princ 350) -> Payment remaining: 0
            // Inst 4 & 5: Unpaid
            decimal payment = 4000m;

            var result = engine.AllocatePayment(installments, payment);

            AssertEqual(300m + 200m + 100m, result.TotalPenaltyPaid, "TotalPenaltyPaid");
            AssertEqual(200m + 150m + 100m, result.TotalOverdueInterestPaid, "TotalOverdueInterestPaid");
            AssertEqual(100m + 100m + 100m, result.TotalCurrentInterestPaid, "TotalCurrentInterestPaid");
            AssertEqual(1000m + 1000m + 650m, result.TotalPrincipalPaid, "TotalPrincipalPaid");
            AssertEqual(0m, result.OverpaymentAdvance, "OverpaymentAdvance");

            AssertEqual(0m, result.RemainingInstallments[0].Principal, "Inst 1 Principal");
            AssertEqual(0m, result.RemainingInstallments[1].Principal, "Inst 2 Principal");
            AssertEqual(350m, result.RemainingInstallments[2].Principal, "Inst 3 Principal");
            AssertEqual(1000m, result.RemainingInstallments[3].Principal, "Inst 4 Principal");
            AssertEqual(1000m, result.RemainingInstallments[4].Principal, "Inst 5 Principal");
        });

        RunTest("Test 12: Stress & Scalability Test (100,000 Installments, Constraint 10^5)", () =>
        {
            const int count = 100_000;
            var installments = new List<Installment>(count);
            var baseDate = new DateTime(2020, 1, 1);

            for (int i = 0; i < count; i++)
            {
                installments.Add(new Installment(
                    i + 1,
                    baseDate.AddDays(i),
                    50m,
                    30m,
                    20m,
                    1000m
                ));
            }

            // Pay 55,000 installments completely (55,000 * 1100 = 60,500,000)
            decimal payment = 60_500_000m;

            var perfSw = Stopwatch.StartNew();
            var result = engine.AllocatePayment(installments, payment);
            perfSw.Stop();

            AssertEqual(55_000 * 50m, result.TotalPenaltyPaid, "TotalPenaltyPaid");
            AssertEqual(55_000 * 30m, result.TotalOverdueInterestPaid, "TotalOverdueInterestPaid");
            AssertEqual(55_000 * 20m, result.TotalCurrentInterestPaid, "TotalCurrentInterestPaid");
            AssertEqual(55_000 * 1000m, result.TotalPrincipalPaid, "TotalPrincipalPaid");
            AssertEqual(0m, result.OverpaymentAdvance, "OverpaymentAdvance");
            AssertEqual(count, result.RemainingInstallments.Count, "RemainingInstallments Count");

            Console.WriteLine($"   ⏱️  Processed 100,000 installments in {perfSw.ElapsedMilliseconds} ms ({perfSw.ElapsedTicks} ticks)");
        });

        sw.Stop();
        PrintFooter(sw.ElapsedMilliseconds);
    }

    // -------------------------------------------------------------
    // Assertion & Reporting Helpers
    // -------------------------------------------------------------
    private static void RunTest(string testName, Action testAction)
    {
        Console.WriteLine($"\n▶ {testName}");
        try
        {
            testAction();
            _passedCount++;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  [PASS] ✓ Test passed successfully");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            _failedCount++;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  [FAIL] ✗ {ex.Message}");
            Console.ResetColor();
        }
    }

    private static void AssertEqual<T>(T expected, T actual, string fieldName)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new Exception($"Assertion Failed for '{fieldName}': Expected <{expected}>, but got <{actual}>");
        }
    }

    private static void PrintHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔═══════════════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║            MULTI-TIER LOAN PAYMENT ALLOCATION (WATERFALL ENGINE) TEST RUNNER         ║");
        Console.WriteLine("║                      LeetCode #1021-T • Banking Core Loan System                      ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
    }

    private static void PrintAllocationSummary(AllocationResult result, decimal payment)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("   ┌─────────────────────────────────────────────────────────────┐");
        Console.WriteLine($"   │ Total Payment Received:    {payment,16:C2}                 │");
        Console.WriteLine($"   │ ├─ Penalty Fee Paid:       {result.TotalPenaltyPaid,16:C2}                 │");
        Console.WriteLine($"   │ ├─ Overdue Interest Paid:  {result.TotalOverdueInterestPaid,16:C2}                 │");
        Console.WriteLine($"   │ ├─ Current Interest Paid:  {result.TotalCurrentInterestPaid,16:C2}                 │");
        Console.WriteLine($"   │ └─ Principal Paid:         {result.TotalPrincipalPaid,16:C2}                 │");
        Console.WriteLine($"   │ Overpayment / Advance:     {result.OverpaymentAdvance,16:C2}                 │");
        Console.WriteLine("   └─────────────────────────────────────────────────────────────┘");
        Console.ResetColor();
    }

    private static void PrintFooter(long totalElapsedMs)
    {
        Console.WriteLine("\n" + new string('=', 87));
        if (_failedCount == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"🎉 ALL TESTS PASSED! Total: {_passedCount + _failedCount} | Passed: {_passedCount} | Failed: {_failedCount} ({totalElapsedMs} ms)");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ SOME TESTS FAILED! Total: {_passedCount + _failedCount} | Passed: {_passedCount} | Failed: {_failedCount} ({totalElapsedMs} ms)");
        }
        Console.ResetColor();
        Console.WriteLine(new string('=', 87));
    }
}
