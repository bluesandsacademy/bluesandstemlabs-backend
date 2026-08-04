namespace BlueSandsLMS.Application.Emails
{
    public static class EmailTemplates
    {
        public static string BuildWelcomeEmailHtml(
            string role, string firstName,
                          string verifyLink,
            string supportEmail, string supportPhone, string appName = "Blue Sands STEM Labs")
        {
            string roleBlock = role switch
            {
                "Teacher" => @"<p><strong>For Teachers:</strong><br/>
Simplify lesson delivery with ready-to-use digital experiments.<br/>
Save classroom time while enriching student engagement.<br/>
Track student participation and progress with ease.</p>",
                "SchoolAdmin" or "Admin" or "GlobalAdmin" => @"<p><strong>For Schools &amp; Administrators:</strong><br/>
Affordable, scalable solution that eliminates lab equipment barriers.<br/>
Strengthens your school's STEM profile and attracts parents.<br/>
Easy integration with your existing curriculum and teaching plans.</p>",
                _ => @"<p><strong>For Students:</strong><br/>
Perform Physics, Chemistry, and Biology practicals virtually.<br/>
Access experiments offline or with little internet.<br/>
Boost confidence and exam readiness with real-world simulations.</p>"
            };

            var encodedApp = System.Net.WebUtility.HtmlEncode(appName);

            return $@"<!doctype html><html><body style=""font-family:Arial,Helvetica,sans-serif;color:#111;line-height:1.6"">
<h2>🎉 Welcome to {encodedApp} – The Future of Learning Awaits!</h2>
<p>Dear {System.Net.WebUtility.HtmlEncode(firstName)},</p>
<p>Welcome aboard! 👋 We're thrilled to have you join {encodedApp}.</p>
<p>With over 7,000+ learners and educators already exploring STEM beyond limits, you're now part of a growing community that believes every student deserves access to hands-on learning — anytime, anywhere.</p>
<hr/>
<h3>🌟 What You Can Expect</h3>
{roleBlock}
<hr/>
<h3>🚀 Your Next Steps</h3>
<ol>
  <li>Explore your personalized dashboard.</li>
  <li>Start your first virtual experiment today!</li>
</ol>
<p style=""margin:16px 0;padding:12px 16px;background:#f5f7ff;border-radius:8px;border:1px solid #e2e6ff;"">
<b>Verify your email:</b> <a href=""{verifyLink}"">Click to verify</a>
</p>
<p>If you need help, our support team is always available to guide you.</p>
<hr/>
<h3>💡 Together, We're Shaping Africa's Future in STEM</h3>
<p>Thank you for choosing to be part of this journey — your role as a student, teacher, or school is vital in building the next generation of scientists, innovators, and problem solvers.</p>
<p><b>Need help?</b><br/>
📩 Email us: <a href=""mailto:{System.Net.WebUtility.HtmlEncode(supportEmail)}"">{System.Net.WebUtility.HtmlEncode(supportEmail)}</a><br/>
📞 Call: {System.Net.WebUtility.HtmlEncode(supportPhone)}</p>
<p>Once again, welcome to the future of STEM learning. Let's explore, experiment, and excel — together!</p>
<p>Warm regards,</p>
<p><b>{encodedApp} Team</b></p>
</body></html>";
        }

        public static string BuildPasswordResetEmailHtml(
            string firstName, string resetLink, string supportEmail, string supportPhone,
            string appName = "Blue Sands STEM Labs")
        {
            var encodedApp = System.Net.WebUtility.HtmlEncode(appName);
            return $@"<!DOCTYPE html>
<html>
<body style=""font-family:Arial,Helvetica,sans-serif;color:#111;line-height:1.6;max-width:600px;margin:0 auto;padding:20px;"">
    <h2 style=""color:#1f2937;"">🔐 Reset Your Password</h2>
    <p>Hello {System.Net.WebUtility.HtmlEncode(firstName)}!</p>
    <p>We received a request to reset your {encodedApp} password. Click the button below to create a new password:</p>
    <p style=""margin:30px 0;text-align:center;"">
        <a href=""{resetLink}"" style=""display:inline-block;background:#3b82f6;color:#fff;padding:14px 28px;text-decoration:none;border-radius:6px;font-weight:bold;font-size:16px;"">
            Reset My Password
        </a>
    </p>
    <p style=""color:#6b7280;font-size:14px;background:#f3f4f6;padding:12px;border-radius:4px;"">
        ⏰ <strong>Important:</strong> This link expires in <strong>1 hour</strong> for security.
    </p>
    <p style=""margin:20px 0;padding:14px;background:#fef3c7;border-left:4px solid #f59e0b;border-radius:4px;"">
        <strong style=""color:#92400e;"">⚠️ Security Notice</strong><br/>
        <span style=""color:#78350f;"">If you didn't request this password reset, you can safely ignore this email. Your password will remain unchanged.</span>
    </p>
    <div style=""margin-top:30px;padding-top:20px;border-top:1px solid #e5e7eb;"">
        <p style=""color:#6b7280;font-size:13px;margin-bottom:8px;"">If the button doesn't work, copy and paste this link into your browser:</p>
        <p style=""word-break:break-all;color:#3b82f6;font-size:12px;background:#f9fafb;padding:8px;border-radius:4px;"">{resetLink}</p>
    </div>
    <hr style=""margin:30px 0;border:none;border-top:1px solid #e5e7eb;""/>
    <p style=""font-size:14px;color:#6b7280;"">
        <strong>Need help?</strong><br/>
        📩 <a href=""mailto:{System.Net.WebUtility.HtmlEncode(supportEmail)}"" style=""color:#3b82f6;text-decoration:none;"">{System.Net.WebUtility.HtmlEncode(supportEmail)}</a><br/>
        📞 {System.Net.WebUtility.HtmlEncode(supportPhone)}
    </p>
    <p style=""font-size:12px;color:#9ca3af;margin-top:30px;padding-top:20px;border-top:1px solid #f3f4f6;"">
        <strong>{encodedApp}</strong>
    </p>
</body>
</html>";
        }

        public static string BuildPasswordChangedEmailHtml(
            string firstName, string loginLink, string supportEmail, string supportPhone,
            string appName = "Blue Sands STEM Labs")
        {
            var encodedApp = System.Net.WebUtility.HtmlEncode(appName);
            return $@"<!DOCTYPE html>
<html>
<body style=""font-family:Arial,Helvetica,sans-serif;color:#111;line-height:1.6;max-width:600px;margin:0 auto;padding:20px;"">
    <h2 style=""color:#1f2937;"">✅ Password Changed Successfully</h2>
    <p>Hello {System.Net.WebUtility.HtmlEncode(firstName)}!</p>
    <p style=""padding:14px;background:#d1fae5;color:#065f46;border-radius:6px;border-left:4px solid #10b981;font-weight:500;"">
        ✓ Your password has been changed successfully!
    </p>
    <p>You can now log in to your {encodedApp} account with your new password.</p>
    <p style=""margin:30px 0;text-align:center;"">
        <a href=""{loginLink}"" style=""display:inline-block;background:#10b981;color:#fff;padding:14px 28px;text-decoration:none;border-radius:6px;font-weight:bold;font-size:16px;"">
            Log In Now
        </a>
    </p>
    <p style=""margin:25px 0;padding:14px;background:#fee2e2;border-left:4px solid #ef4444;border-radius:4px;"">
        <strong style=""color:#991b1b;"">🚨 Didn't make this change?</strong><br/>
        <span style=""color:#b91c1c;"">If you did not request this password change, your account may be compromised. Please contact our support team <strong>immediately</strong>:</span>
        <br/><br/>
        📩 <a href=""mailto:{System.Net.WebUtility.HtmlEncode(supportEmail)}"" style=""color:#dc2626;font-weight:bold;"">{System.Net.WebUtility.HtmlEncode(supportEmail)}</a><br/>
        📞 <strong>{System.Net.WebUtility.HtmlEncode(supportPhone)}</strong>
    </p>
    <div style=""margin:30px 0;padding:16px;background:#f9fafb;border-radius:6px;border:1px solid #e5e7eb;"">
        <p style=""font-size:14px;color:#374151;margin:0 0 10px 0;""><strong>🔒 Security Tips:</strong></p>
        <ul style=""font-size:13px;color:#6b7280;margin:0;padding-left:20px;"">
            <li style=""margin-bottom:6px;"">Use a unique, strong password for your account</li>
            <li style=""margin-bottom:6px;"">Never share your password with anyone</li>
            <li style=""margin-bottom:6px;"">Be cautious of phishing emails asking for your password</li>
            <li>Log out from shared devices after use</li>
        </ul>
    </div>
    <hr style=""margin:30px 0;border:none;border-top:1px solid #e5e7eb;""/>
    <p style=""font-size:14px;color:#6b7280;"">
        <strong>Need help?</strong><br/>
        📩 <a href=""mailto:{System.Net.WebUtility.HtmlEncode(supportEmail)}"" style=""color:#3b82f6;text-decoration:none;"">{System.Net.WebUtility.HtmlEncode(supportEmail)}</a><br/>
        📞 {System.Net.WebUtility.HtmlEncode(supportPhone)}
    </p>
    <p style=""font-size:12px;color:#9ca3af;margin-top:30px;padding-top:20px;border-top:1px solid #f3f4f6;"">
        <strong>{encodedApp}</strong>
    </p>
</body>
</html>";
        }
    }
}
