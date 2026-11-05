### Fixed
- **Adaptive CAT Quiz Option Matching**: Sửa lỗi logic trong `TestViewModel.cs` khi `selectedOptIndex` mặc định là `0` thay vì `-1`, ngăn ngừa câu hỏi typing chưa khớp hoặc bị skip vô tình được tính là đúng nếu đáp án đúng nằm ở index 0.
- **Skipped Question Scoring & Evaluation Counter**: Sửa luồng `SubmitAnswer` khi người dùng bấm "Next Question" mà chưa chọn/nhập đáp án, hỗ trợ `SubmitAnswerInternal(allowEmpty: true)` để ghi nhận điểm 0 và bảo toàn số lượng đánh giá bài thi.
- **Adaptive Progress Index Alignment**: Căn chỉnh `CurrentQuestionIndex` trong `LoadAdaptiveQuestion` về chuẩn 0-based (`adaptiveQ.QuestionIndex - 1`) để đồng bộ với định dạng hiển thị `Quiz_ProgressFormat` chuẩn xác từ câu 1.

### Changed
- **Dynamic Last Question Action Button**: Tại câu hỏi cuối cùng của bài kiểm tra, nút điều hướng trong `TestPage.xaml` tự động đổi từ "Câu tiếp theo" (`Test_NextQuestion`) sang "Hoàn thành bài thi" (`Test_FinishTest`) kèm icon checkmark `\uE73E`, mang lại trải nghiệm tương tác rõ ràng, chuyên nghiệp cho người dùng trước khi nộp bài.
