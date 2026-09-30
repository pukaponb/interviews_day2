# 1. LeetCode #1021-T: Multi-Tier Loan Payment Allocation (Waterfall Engine)

---

### 📜 คำอธิบายโจทย์ (Problem Statement)

ในระบบชำระสินเชื่อของสถาบันการเงิน (เช่น SCB Loan Payment) เมื่อลูกค้านำส่งเงินก้อน paymentAmount เข้ามาเพื่อตัดชำระหนี้ข้ามหลายงวดค้างชำระ (LoanInstallment[]) ระบบต้องจัดสรรเงินตัดชำระหนี้ตามกฎหมายและลำดับความสำคัญแบบ Waterfall ดังนี้:

จงเขียนฟังก์ชัน AllocatePayment(List<Installment> installments, decimal paymentAmount) ที่คืนค่ารายงานสรุปยอดเงินที่ตัดในแต่ละประเภท ยอดหนี้คงเหลือของแต่ละงวด และเงินคงเหลือส่วนเกิน

1. เรียงลำดับการตัดหนี้จาก งวดที่ครบกำหนดชำระก่อน (Oldest Due Date) ไปยังงวดล่าสุด
2. ในแต่ละงวด ต้องตัดหนี้ตามลำดับขั้น: 1. ค่าปรับผิดนัด (Penalty Fee) ➔ 2. ดอกเบี้ยค้างชำระ (Overdue Interest) ➔ 3. ดอกเบี้ยรอบปัจจุบัน (Accrued Interest) ➔ 4. เงินต้น (Principal)
3. หากเงินชำระหมดในขั้นตอนใด ให้หยุดการตัด และบันทึกยอดหนี้คงค้างในแต่ละส่วนของงวดนั้นๆ
4. หากตัดหนี้ครบทุกงวดแล้วยังมีเงินเหลือ ให้เก็บยอดเงินส่วนเกินเป็น ยอดชำระล่วงหน้า (Overpayment / Advance Balance)

### ตัวอย่างที่ 1 (Example 1):

```text
Input:
installments = [
{ Id: 1, DueDate: "2026-01-01", Penalty: 500, OverdueInterest: 300, CurrentInterest: 200, Principal: 5000 },
{ Id: 2, DueDate: "2026-02-01", Penalty: 0, OverdueInterest: 0, CurrentInterest: 200, Principal: 5000 }
]
paymentAmount = 1500
Output:
Allocated: { PenaltyPaid: 500, OverdueInterestPaid: 300, CurrentInterestPaid: 200, PrincipalPaid: 500 }
RemainingInstallment_1: { Penalty: 0, OverdueInterest: 0, CurrentInterest: 0, Principal: 4500 }
RemainingInstallment_2: { Penalty: 0, OverdueInterest: 0, CurrentInterest: 200, Principal: 5000 }
Overpayment: 0
```

### 📌 ข้อจำกัด (Constraints)

- 1 <= installments.Length <= 10^5
- 0.00 <= paymentAmount <= 10^9
- จำนวนเงินทุกช่องเป็นค่าบวก (>= 0) และต้องใช้ความแม่นยำระดับทศนิยม (decimal) ป้องกัน Floating-point rounding errors
