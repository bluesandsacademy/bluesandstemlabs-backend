namespace BlueSandsLMS.Application.Emails
{
    public static class EmailTemplates
    {
        public static string BuildWelcomeEmailHtml(
            string role, string firstName, string loginLink, string verifyLink, string supportEmail, string supportPhone)
        {
            string roleBlock = role switch
            {
                "Teacher" => @"<p><strong>For Teachers:</strong><br/>
Simplify lesson delivery with ready-to-use digital experiments.<br/>
Save classroom time while enriching student engagement.<br/>
Track student participation and progress with ease.</p>",

                "SchoolAdmin" or "Admin" or "GlobalAdmin" => @"<p><strong>For Schools &amp; Administrators:</strong><br/>
Affordable, scalable solution that eliminates lab equipment barriers.<br/>
Strengthens your school’s STEM profile and attracts parents.<br/>
Easy integration with your existing curriculum and teaching plans.</p>",

                _ => @"<p><strong>For Students:</strong><br/>
Perform Physics, Chemistry, and Biology practicals virtually.<br/>
Access experiments offline or with little internet.<br/>
Boost confidence and exam readiness with real-world simulations.</p>"
            };

            return $@"<!doctype html><html><body style=""font-family:Arial,Helvetica,sans-serif;color:#111;line-height:1.6"">
<h2>🎉 Welcome to Blue Sands STEM Labs – The Future of Learning Awaits!</h2>

<p>Dear {System.Net.WebUtility.HtmlEncode(firstName)},</p>

<p>Welcome aboard! 👋 We’re thrilled to have you join Blue Sands STEM Labs, a groundbreaking solution developed by Blue Sands Academy Limited to transform science education across Nigeria and Africa.</p>

<p>With over 7,000+ learners and educators already exploring STEM beyond limits, you’re now part of a growing community that believes every student deserves access to hands-on learning — anytime, anywhere.</p>

<hr/>

<h3>🌟 What You Can Expect</h3>
{roleBlock}

<hr/>

<h3>🚀 Your Next Steps</h3>
<ol>
  <li>Log in to your account: <a href=""{loginLink}"">Login Link</a></li>
  <li>Explore your personalized dashboard.</li>
  <li>Start your first virtual experiment today!</li>
</ol>

<p style=""margin:16px 0;padding:12px 16px;background:#f5f7ff;border-radius:8px;border:1px solid #e2e6ff;"">
<b>Verify your email:</b> <a href=""{verifyLink}"">Click to verify</a>
</p>

<p>If you need help, our support team is always available to guide you.</p>

<hr/>

<h3>💡 Together, We’re Shaping Africa’s Future in STEM</h3>
<p>At Blue Sands, we believe innovation should be within reach for every learner.</p>

<p>Thank you for choosing to be part of this journey — your role as a student, teacher, or school is vital in building the next generation of scientists, innovators, and problem solvers.</p>

<p><b>Need help?</b><br/>
📩 Email us: <a href=""mailto:{System.Net.WebUtility.HtmlEncode(supportEmail)}"">{System.Net.WebUtility.HtmlEncode(supportEmail)}</a><br/>
📞 Call: {System.Net.WebUtility.HtmlEncode(supportPhone)}</p>

<p>Once again, welcome to the future of STEM learning. Let’s explore, experiment, and excel — together!</p>

<p>Warm regards,</p>
<p><b>Alero Thompson</b><br/>
Co-Founder/CEO<br/>
Blue Sands STEM Labs Team</p>
</body></html>";
        }
    }
}
