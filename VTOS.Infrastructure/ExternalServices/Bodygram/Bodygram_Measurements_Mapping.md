# Bodygram Measurements Mapping

Maps sinh phục giữa danh sách cần đo của người dùng và các field trong response JSON từ Bodygram API.

## Upper Body & Arms (Phần trên & tay)

| Tên Tiếng Anh | Tên Tiếng Việt | JSON Field Name | Unit | Description |
|---|---|---|---|---|
| Waist | Eo | `waistGirth` | mm | Chu vi eo |
| Bust | Ngực | `bustGirth` | mm | Chu vi ngực đầy đủ |
| Upper Arm | Tay trên | `upperArmGirthR` | mm | Chu vi tay trên (bên phải) |
| Neck | Cổ | `neckGirth` | mm | Chu vi cổ |
| Neck Base | Gốc cổ | `neckBaseGirth` | mm | Chu vi gốc cổ |
| Shoulder Width | Chiều rộng vai | `acrossBackShoulderWidth` | mm | Khoảng cách từ vai trái sang vai phải |
| Wrist | Cổ tay | `wristGirthR` | mm | Chu vi cổ tay (bên phải) |
| Sleeve Length | Chiều dài tay áo | `backNeckPointToWristLengthR` | mm | Khoảng cách từ gốc cổ đến cổ tay (bên phải) |
| Outer Arm Length | Chiều dài tay ngoài | `outerArmLengthR` | mm | Chiều dài tay ngoài từ vai đến cổ tay (bên phải) |
| Under Bust | Dưới ngực | `underBustGirth` | mm | Chu vi dưới ngực |
| Belly Waist | Eo bụng | `bellyWaistGirth` | mm | Chu vi eo bụng |
| Back Length | Chiều dài lưng | `backNeckPointToGroundContoured` | mm | Chiều dài từ gốc cổ xuống mặt đất (theo đường cong) |

---

## Lower Body & Legs (Phần dưới & chân)

| Tên Tiếng Anh | Tên Tiếng Việt | JSON Field Name | Unit | Description |
|---|---|---|---|---|
| Hip | Hông | `hipGirth` | mm | Chu vi hông |
| Top Hip | Hông trên | `topHipGirth` | mm | Chu vi hông phần trên |
| Thigh | Đùi | `thighGirthR` | mm | Chu vi đùi (bên phải) |
| Mid Thigh | Giữa đùi | `midThighGirthR` | mm | Chu vi ở giữa đùi (bên phải) |
| Knee | Gối | `kneeGirthR` | mm | Chu vi gối (bên phải) |
| Calf | Bắp chân | `calfGirthR` | mm | Chu vi bắp chân (bên phải) |
| Outside Leg Length | Chiều dài chân ngoài | `outsideLegLengthR` | mm | Khoảng cách từ hông đến mặt đất ngoài người (bên phải) |
| Inside Leg Length | Chiều dài chân trong | `insideLegLengthR` | mm | Khoảng cách từ crotch đến mặt đất trong (bên phải) |
| Inside Leg Height | Chiều cao chân trong | `insideLegHeight` | mm | Độ cao chân trong từ mặt đất |
| Outer Ankle Height | Chiều cao mắt cá ngoài | `outerAnkleHeightR` | mm | Độ cao mắt cá ngoài từ mặt đất (bên phải) |
| Outseam | Độ dài đường chạy ngoài | `outseamR` | mm | Khoảng cách từ eo đến mắt cá ngoài (bên phải) |

---

## Additional Measurements (Số đo bổ sung)

| Tên Tiếng Anh | Tên Tiếng Việt | JSON Field Name | Unit | Description |
|---|---|---|---|---|
| Back Neck Height | Chiều cao cổ lưng | `backNeckHeight` | mm | Chiều cao của điểm cổ lưng từ mặt đất |
| Back Neck Point to Waist | Từ cổ lưng đến eo | `backNeckPointToWaist` | mm | Khoảng cách từ điểm cổ lưng đến eo |
| Belly Waist Depth | Sâu eo bụng | `bellyWaistDepth` | mm | Độ sâu eo bụng (hình chiếu) |
| Belly Waist Width | Chiều rộng eo bụng | `bellyWaistWidth` | mm | Chiều rộng eo bụng |
| Belly Waist Height | Chiều cao eo bụng | `bellyWaistHeight` | mm | Chiều cao điểm eo bụng từ mặt đất |
| Bust Height | Chiều cao ngực | `bustHeight` | mm | Chiều cao điểm ngực từ mặt đất |
| Forearm Girth | Chu vi cẳng tay | `forearmGirthR` | mm | Chu vi cẳng tay (bên phải) |
| Hip Height | Chiều cao hông | `hipHeight` | mm | Chiều cao điểm hông từ mặt đất |
| Knee Height | Chiều cao gối | `kneeHeightR` | mm | Chiều cao gối từ mặt đất (bên phải) |
| Shoulder to Elbow | Từ vai đến khuỷu | `shoulderToElbowR` | mm | Khoảng cách từ vai đến khuỷu (bên phải) |
| Top Hip Height | Chiều cao hông trên | `topHipHeight` | mm | Chiều cao điểm hông trên từ mặt đất |
| Waist Height | Chiều cao eo | `waistHeight` | mm | Chiều cao điểm eo từ mặt đất |

---

## Notes

- Tất cả các measurement đều được trả về từ Bodygram API dưới dạng số nguyên (integer) với đơn vị là **milimeters (mm)**
- Các field có suffix **_R** đại diện cho phía bên **phải** của cơ thể (Right side)
- Để chuyển đổi từ mm sang cm: chia cho 10
- Để chuyển đổi từ mm sang m: chia cho 1000
- "Contoured" có nghĩa là đo theo đường cong tự nhiên của cơ thể

---

## Usage Example

```csharp
// Access measurements từ BodygramScanResponse
var scanResponse = await _bodygramService.GetScanAsync(scanId);
var entry = scanResponse?.Entry;

if (entry?.Measurements != null)
{
    // Lấy chu vi eo
    var waistMeasurement = entry.Measurements.FirstOrDefault(m => m.Name == "waistGirth");
    if (waistMeasurement != null)
    {
        var waistInCm = waistMeasurement.Value / 10;
        Console.WriteLine($"Waist: {waistInCm} cm");
    }
    
    // Lấy chiều dài chân trong
    var insideLegLength = entry.Measurements.FirstOrDefault(m => m.Name == "insideLegLengthR");
    if (insideLegLength != null)
    {
        var inseamInCm = insideLegLength.Value / 10;
        Console.WriteLine($"Inside Leg Length: {inseamInCm} cm");
    }
}
```
