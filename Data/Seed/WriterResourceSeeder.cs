using ScholarRescue.Models;
using ScholarRescue.Models.Enums;

namespace ScholarRescue.Data.Seed
{
    /// <summary>
    /// Seeds initial Writer Knowledge Center content across all categories.
    /// </summary>
    public static class WriterResourceSeeder
    {
        public static async Task SeedAsync(ScholarRescueDbContext context)
        {
            if (context.WriterResources.Any())
                return;

            var resources = new List<WriterResource>();

            // ────────────────────────────────────────
            // FAQ
            // ────────────────────────────────────────
            resources.Add(new WriterResource
            {
                Title = "How do I apply to an order?",
                Question = "How do I apply to an order?",
                Content = "<p>As an approved writer, you can browse available orders from the <strong>Available Orders</strong> marketplace. Click on any order to view its details, then use the <strong>Apply</strong> button to submit your application.</p><p>The admin team will review your application and assign the order to the most suitable writer.</p>",
                Category = WriterResourceCategory.FAQ,
                SubCategory = "Orders",
                Tags = "apply, orders, marketplace",
                SortOrder = 1,
                IsActive = true
            });

            resources.Add(new WriterResource
            {
                Title = "How long does order assignment take?",
                Question = "How long does order assignment take?",
                Content = "<p>Order assignments are typically processed within <strong>24–48 hours</strong> after a writer applies. Admins review all applications and select the best-qualified writer based on the order requirements.</p>",
                Category = WriterResourceCategory.FAQ,
                SubCategory = "Orders",
                Tags = "assignment, timeline, selection",
                SortOrder = 2,
                IsActive = true
            });

            resources.Add(new WriterResource
            {
                Title = "How do I submit completed work?",
                Question = "How do I submit completed work?",
                Content = "<p>Once assigned to an order, navigate to the order details page and use the <strong>Submit Work</strong> section. Upload your completed document and select the submission type (Draft, Revision, or Final).</p><p>The client will be notified and can review your submission.</p>",
                Category = WriterResourceCategory.FAQ,
                SubCategory = "Orders",
                Tags = "submit, upload, deliver",
                SortOrder = 3,
                IsActive = true
            });

            resources.Add(new WriterResource
            {
                Title = "When do I get paid?",
                Question = "When do I get paid?",
                Content = "<p>Writer earnings are credited to your wallet upon order completion. You can request a payout during the payout window (1st and 15th of each month). Your earnings are calculated at <strong>90%</strong> of the order budget, with a 10% platform commission.</p>",
                Category = WriterResourceCategory.FAQ,
                SubCategory = "Payments",
                Tags = "payment, payout, earnings, wallet",
                SortOrder = 4,
                IsActive = true
            });

            resources.Add(new WriterResource
            {
                Title = "What is the revision policy?",
                Question = "What is the revision policy?",
                Content = "<p>Clients can request revisions if the delivered work does not meet the original requirements. As a writer, you will receive a notification with the revision details. You should address the feedback and resubmit the revised work promptly.</p><p>Multiple revision rounds may occur before the client accepts the final submission.</p>",
                Category = WriterResourceCategory.FAQ,
                SubCategory = "Revisions",
                Tags = "revision, resubmit, client feedback",
                SortOrder = 5,
                IsActive = true
            });

            resources.Add(new WriterResource
            {
                Title = "How do I update my payment details?",
                Question = "How do I update my payment details?",
                Content = "<p>Go to <strong>Payment Details</strong> in the sidebar under Finances. You can add or update your payment method (bank transfer, M-Pesa, or PayPal) and save your account information for payouts.</p>",
                Category = WriterResourceCategory.FAQ,
                SubCategory = "Account",
                Tags = "payment details, account, payout method",
                SortOrder = 6,
                IsActive = true
            });

            // ────────────────────────────────────────
            // Writer Rules
            // ────────────────────────────────────────
            resources.Add(new WriterResource
            {
                Title = "Platform Policies",
                Content = "<h3>Platform Policies</h3><ul><li><strong>Confidentiality:</strong> All order details, client information, and completed work must remain strictly confidential.</li><li><strong>Academic Integrity:</strong> All work must be original and free from plagiarism. Submissions are checked automatically.</li><li><strong>Communication:</strong> All communication with clients must go through the platform messaging system.</li><li><strong>Professionalism:</strong> Maintain a professional tone in all interactions. Respect deadlines and client requirements.</li><li><strong>Account Sharing:</strong> Your writer account is non-transferable. Do not share login credentials.</li></ul>",
                Category = WriterResourceCategory.WriterRules,
                SubCategory = "Platform Policies",
                Tags = "policy, confidentiality, integrity, professionalism",
                SortOrder = 1,
                IsActive = true
            });

            resources.Add(new WriterResource
            {
                Title = "Assignment Policies",
                Content = "<h3>Assignment Policies</h3><ul><li><strong>Application Process:</strong> Writers apply to orders through the marketplace. Admins review and assign based on qualifications.</li><li><strong>Deadline Compliance:</strong> Once assigned, you must complete the work by the specified deadline. Failure to meet deadlines may result in penalties.</li><li><strong>Scope Changes:</strong> If the order scope significantly changes, contact admin support for assistance.</li><li><strong>Declining Orders:</strong> If you cannot complete an assigned order, notify admin immediately. Repeated cancellations may affect your standing.</li></ul>",
                Category = WriterResourceCategory.WriterRules,
                SubCategory = "Assignment Policies",
                Tags = "assignment, deadline, application, cancellation",
                SortOrder = 2,
                IsActive = true
            });

            resources.Add(new WriterResource
            {
                Title = "Revision Policies",
                Content = "<h3>Revision Policies</h3><ul><li><strong>Free Revisions:</strong> Writers are expected to address reasonable revision requests within the scope of the original order requirements.</li><li><strong>Scope Creep:</strong> If revision requests go beyond the original order scope, you may escalate to admin for review.</li><li><strong>Response Time:</strong> Respond to revision requests within 24 hours. Communicate proactively if more time is needed.</li><li><strong>Documentation:</strong> Always reference the specific feedback addressed in each revision round.</li></ul>",
                Category = WriterResourceCategory.WriterRules,
                SubCategory = "Revision Policies",
                Tags = "revision, scope, response time",
                SortOrder = 3,
                IsActive = true
            });

            resources.Add(new WriterResource
            {
                Title = "Payout Policies",
                Content = "<h3>Payout Policies</h3><ul><li><strong>Earnings Split:</strong> Writers receive 90% of the order budget. A 10% platform commission applies.</li><li><strong>Payout Windows:</strong> Payout requests are processed on the <strong>1st</strong> and <strong>15th</strong> of each month.</li><li><strong>Minimum Payout:</strong> The minimum payout amount is $25.</li><li><strong>Payment Methods:</strong> We support bank transfer, M-Pesa, and PayPal.</li><li><strong>Processing Time:</strong> Approved payouts are processed within 3–5 business days.</li></ul>",
                Category = WriterResourceCategory.WriterRules,
                SubCategory = "Payout Policies",
                Tags = "payout, commission, payment, earnings",
                SortOrder = 4,
                IsActive = true
            });

            // ────────────────────────────────────────
            // Writing Guide
            // ────────────────────────────────────────
            resources.Add(new WriterResource
            {
                Title = "Essay Writing Guide",
                Content = "<h3>Essay Writing</h3><p>A well-structured essay follows these key components:</p><ol><li><strong>Introduction:</strong> Hook the reader, provide context, and present a clear thesis statement.</li><li><strong>Body Paragraphs:</strong> Each paragraph should have a topic sentence, evidence, analysis, and a transition.</li><li><strong>Conclusion:</strong> Restate the thesis, summarize key points, and provide broader implications.</li></ol><h4>Tips</h4><ul><li>Use active voice wherever possible.</li><li>Avoid contractions in academic writing.</li><li>Vary sentence structure for better readability.</li><li>Always cite sources properly using the required citation style.</li></ul>",
                Category = WriterResourceCategory.WritingGuide,
                SubCategory = "Essay Writing",
                Tags = "essay, structure, thesis, introduction, conclusion",
                SortOrder = 1,
                IsActive = true
            });

            resources.Add(new WriterResource
            {
                Title = "Research Papers Guide",
                Content = "<h3>Research Papers</h3><p>Research papers require systematic investigation and evidence-based arguments:</p><ol><li><strong>Title Page:</strong> Include the paper title, author, institution, and date.</li><li><strong>Abstract:</strong> A concise summary (150–250 words) of the research purpose, methods, findings, and conclusions.</li><li><strong>Introduction:</strong> Background, research question, and significance of the study.</li><li><strong>Literature Review:</strong> Critical analysis of existing research relevant to your topic.</li><li><strong>Methodology:</strong> Detailed description of research design, data collection, and analysis methods.</li><li><strong>Results:</strong> Present findings with tables, charts, and statistical analysis.</li><li><strong>Discussion:</strong> Interpret results, compare with existing literature, acknowledge limitations.</li><li><strong>Conclusion:</strong> Summarize key findings and suggest future research directions.</li></ol>",
                Category = WriterResourceCategory.WritingGuide,
                SubCategory = "Research Papers",
                Tags = "research, methodology, abstract, literature review",
                SortOrder = 2,
                IsActive = true
            });

            resources.Add(new WriterResource
            {
                Title = "Case Studies Guide",
                Content = "<h3>Case Studies</h3><p>Case studies analyze a specific situation, organization, or event in depth:</p><ol><li><strong>Background/Context:</strong> Describe the organization, situation, and key players.</li><li><strong>Problem Statement:</strong> Clearly identify the core issue(s) being analyzed.</li><li><strong>Analysis:</strong> Apply theoretical frameworks and models to examine the problem.</li><li><strong>Alternatives:</strong> Present possible solutions with pros and cons.</li><li><strong>Recommendations:</strong> Propose the best course of action with implementation steps.</li></ol><h4>Key Considerations</h4><ul><li>Use real-world data and evidence where possible.</li><li>Apply relevant academic theories (SWOT, PESTLE, Porter's Five Forces, etc.).</li><li>Maintain objectivity throughout the analysis.</li></ul>",
                Category = WriterResourceCategory.WritingGuide,
                SubCategory = "Case Studies",
                Tags = "case study, analysis, SWOT, recommendations",
                SortOrder = 3,
                IsActive = true
            });

            resources.Add(new WriterResource
            {
                Title = "Dissertation Writing Guide",
                Content = "<h3>Dissertation Writing</h3><p>A dissertation is an extended research project demonstrating mastery of a subject:</p><ol><li><strong>Title Page & Abstract</strong></li><li><strong>Introduction:</strong> Research problem, objectives, significance, and scope.</li><li><strong>Literature Review:</strong> Comprehensive review of existing scholarship.</li><li><strong>Methodology:</strong> Research design, data collection, and analytical approach.</li><li><strong>Results/Findings:</strong> Present data systematically.</li><li><strong>Discussion:</strong> Interpret findings in context of the literature.</li><li><strong>Conclusion & Recommendations:</strong> Summarize contributions and suggest future work.</li><li><strong>References:</strong> Complete bibliography in the required citation style.</li></ol><p><strong>Tip:</strong> Break the dissertation into chapters and set milestone deadlines for each section.</p>",
                Category = WriterResourceCategory.WritingGuide,
                SubCategory = "Dissertations",
                Tags = "dissertation, thesis, extended research",
                SortOrder = 4,
                IsActive = true
            });

            resources.Add(new WriterResource
            {
                Title = "Literature Reviews Guide",
                Content = "<h3>Literature Reviews</h3><p>A literature review synthesizes existing research on a topic:</p><h4>Types</h4><ul><li><strong>Narrative Review:</strong> Thematic summary of existing literature.</li><li><strong>Systematic Review:</strong> Structured, methodical review following strict criteria.</li><li><strong>Meta-Analysis:</strong> Statistical combination of results from multiple studies.</li></ul><h4>Structure</h4><ol><li>Define the research scope and search strategy.</li><li>Organize sources thematically, chronologically, or methodologically.</li><li>Critically evaluate each source for strengths and limitations.</li><li>Identify gaps in the existing literature.</li><li>Synthesize findings to build a coherent narrative.</li></ol>",
                Category = WriterResourceCategory.WritingGuide,
                SubCategory = "Literature Reviews",
                Tags = "literature review, synthesis, systematic review, meta-analysis",
                SortOrder = 5,
                IsActive = true
            });

            // ────────────────────────────────────────
            // Citation Guides
            // ────────────────────────────────────────
            resources.Add(new WriterResource
            {
                Title = "APA 7th Edition Citation Guide",
                Content = "<h3>APA 7th Edition</h3><h4>In-Text Citations</h4><ul><li><strong>Paraphrase:</strong> (Author, Year) — e.g., (Smith, 2024)</li><li><strong>Direct Quote:</strong> (Author, Year, p. #) — e.g., (Smith, 2024, p. 15)</li><li><strong>Two Authors:</strong> (Smith & Jones, 2024)</li><li><strong>3+ Authors:</strong> (Smith et al., 2024)</li></ul><h4>Reference List (Journal Article)</h4><p>Author, A. A., & Author, B. B. (Year). Title of article. <em>Journal Name</em>, <em>volume</em>(issue), pages. https://doi.org/xxxxx</p><h4>Reference List (Book)</h4><p>Author, A. A. (Year). <em>Title of work</em>. Publisher.</p><h4>Reference List (Website)</h4><p>Author, A. A. (Year, Month Day). Title of page. Site Name. URL</p>",
                Category = WriterResourceCategory.CitationGuides,
                SubCategory = "APA 7",
                Tags = "APA, citation, reference, in-text",
                SortOrder = 1,
                IsActive = true
            });

            resources.Add(new WriterResource
            {
                Title = "MLA 9th Edition Citation Guide",
                Content = "<h3>MLA 9th Edition</h3><h4>In-Text Citations</h4><ul><li><strong>Basic:</strong> (Author Page) — e.g., (Smith 25)</li><li><strong>No Author:</strong> (\"Title\" Page)</li><li><strong>Two Authors:</strong> (Smith and Jones 25)</li><li><strong>3+ Authors:</strong> (Smith et al. 25)</li></ul><h4>Works Cited (Book)</h4><p>Author Last, First. <em>Title of Book</em>. Publisher, Year.</p><h4>Works Cited (Journal Article)</h4><p>Author Last, First. \"Title of Article.\" <em>Journal Name</em>, vol. #, no. #, Year, pp. #–#.</p><h4>Works Cited (Website)</h4><p>Author Last, First. \"Title of Page.\" <em>Site Name</em>, Publisher, Date, URL.</p>",
                Category = WriterResourceCategory.CitationGuides,
                SubCategory = "MLA 9",
                Tags = "MLA, citation, works cited",
                SortOrder = 2,
                IsActive = true
            });

            resources.Add(new WriterResource
            {
                Title = "Chicago/Turabian Citation Guide",
                Content = "<h3>Chicago Style (17th Edition)</h3><h4>Notes and Bibliography</h4><ul><li><strong>Footnote:</strong> First Name Last Name, <em>Title of Book</em> (Place: Publisher, Year), page.</li><li><strong>Subsequent:</strong> Last Name, <em>Title</em>, page.</li></ul><h4>Bibliography (Book)</h4><p>Last, First. <em>Title of Book</em>. Place: Publisher, Year.</p><h4>Bibliography (Journal Article)</h4><p>Last, First. \"Title.\" <em>Journal</em> Vol, no. Issue (Year): pages.</p><h4>Author-Date System</h4><ul><li><strong>In-text:</strong> (Last Name Year, page)</li><li><strong>Reference List:</strong> Last, First. Year. <em>Title</em>. Place: Publisher.</li></ul>",
                Category = WriterResourceCategory.CitationGuides,
                SubCategory = "Chicago",
                Tags = "Chicago, Turabian, footnotes, bibliography",
                SortOrder = 3,
                IsActive = true
            });

            resources.Add(new WriterResource
            {
                Title = "Harvard Referencing Guide",
                Content = "<h3>Harvard Referencing</h3><h4>In-Text Citations</h4><ul><li><strong>Basic:</strong> (Author, Year) — e.g., (Smith, 2024)</li><li><strong>Direct Quote:</strong> (Smith, 2024, p. 45)</li><li><strong>Two Authors:</strong> (Smith and Jones, 2024)</li><li><strong>3+ Authors:</strong> (Smith et al., 2024)</li></ul><h4>Reference List (Book)</h4><p>Last Name, Initial(s). (Year) <em>Title of Book</em>. Edition. Place: Publisher.</p><h4>Reference List (Journal Article)</h4><p>Last Name, Initial(s). (Year) 'Title of article', <em>Journal Name</em>, Volume(Issue), pp. pages.</p><h4>Reference List (Website)</h4><p>Last Name, Initial(s). (Year) 'Title of page', <em>Site Name</em>. Available at: URL (Accessed: Date).</p>",
                Category = WriterResourceCategory.CitationGuides,
                SubCategory = "Harvard",
                Tags = "Harvard, referencing, citation",
                SortOrder = 4,
                IsActive = true
            });

            // ────────────────────────────────────────
            // Formatting Guide
            // ────────────────────────────────────────
            resources.Add(new WriterResource
            {
                Title = "Document Formatting Standards",
                Content = "<h3>Document Formatting Standards</h3><h4>General Requirements</h4><ul><li><strong>Font:</strong> Times New Roman, 12pt (or as specified by client)</li><li><strong>Line Spacing:</strong> Double-spaced throughout</li><li><strong>Margins:</strong> 1 inch (2.54 cm) on all sides</li><li><strong>Alignment:</strong> Left-aligned (ragged right edge)</li><li><strong>Page Numbers:</strong> Top right or bottom center</li></ul><h4>Headings</h4><ul><li><strong>Level 1:</strong> Bold, Centered, Title Case</li><li><strong>Level 2:</strong> Bold, Left-aligned, Title Case</li><li><strong>Level 3:</strong> Bold Italic, Left-aligned, Title Case</li></ul><h4>Paragraphs</h4><ul><li>Indent first line of each paragraph by 0.5 inches</li><li>No extra space between paragraphs</li><li>Use transitions between paragraphs for flow</li></ul>",
                Category = WriterResourceCategory.FormattingGuide,
                SubCategory = "General",
                Tags = "formatting, font, margins, spacing, headings",
                SortOrder = 1,
                IsActive = true
            });

            resources.Add(new WriterResource
            {
                Title = "APA Formatting Requirements",
                Content = "<h3>APA Formatting</h3><ul><li><strong>Font:</strong> 12pt Times New Roman, 11pt Calibri, or 10pt Arial</li><li><strong>Spacing:</strong> Double-spaced throughout</li><li><strong>Margins:</strong> 1 inch on all sides</li><li><strong>Title Page:</strong> Title, author name, institutional affiliation, course, instructor, date</li><li><strong>Running Head:</strong> Shortened title in all caps (max 50 characters) on every page</li><li><strong>Page Numbers:</strong> Top right corner</li><li><strong>Headings:</strong> 5 levels — use bold, title case formatting</li><li><strong>Abstract:</strong> 150–250 words on a separate page</li><li><strong>References:</strong> Separate page, hanging indent, alphabetical order</li></ul>",
                Category = WriterResourceCategory.FormattingGuide,
                SubCategory = "APA Formatting",
                Tags = "APA, title page, running head, formatting",
                SortOrder = 2,
                IsActive = true
            });

            // ────────────────────────────────────────
            // Writer Checklist
            // ────────────────────────────────────────
            resources.Add(new WriterResource
            {
                Title = "Plagiarism Checks",
                Content = "<h3>Plagiarism Prevention Checklist</h3><ul><li>☐ All direct quotes are enclosed in quotation marks with proper in-text citations</li><li>☐ Paraphrased content is substantially reworded and restructured (not just synonym replacement)</li><li>☐ All borrowed ideas, even when paraphrased, include citations</li><li>☐ Self-plagiarism is avoided — do not reuse previously submitted work</li><li>☐ References list matches all in-text citations (no missing or extra entries)</li><li>☐ Run the work through a plagiarism detection tool before submission</li><li>☐ Common knowledge facts do not require citation, but verify when in doubt</li><li>☐ Block quotes (40+ words in APA) are properly formatted and cited</li></ul>",
                Category = WriterResourceCategory.WriterChecklist,
                SubCategory = "Plagiarism Checks",
                Tags = "plagiarism, originality, citation, quotation",
                SortOrder = 1,
                IsActive = true
            });

            resources.Add(new WriterResource
            {
                Title = "Formatting Checks",
                Content = "<h3>Formatting Review Checklist</h3><ul><li>☐ Correct font type and size used throughout</li><li>☐ Line spacing is consistent (double-spaced unless otherwise specified)</li><li>☐ Margins are set to 1 inch on all sides</li><li>☐ Page numbers are correctly placed</li><li>☐ Headings follow the correct hierarchy (Level 1, 2, 3)</li><li>☐ Title page includes all required elements</li><li>☐ Paragraphs are properly indented</li><li>☐ Tables and figures are numbered, titled, and referenced in text</li><li>☐ References page is properly formatted with hanging indents</li></ul>",
                Category = WriterResourceCategory.WriterChecklist,
                SubCategory = "Formatting Checks",
                Tags = "formatting, font, margins, headings, title page",
                SortOrder = 2,
                IsActive = true
            });

            resources.Add(new WriterResource
            {
                Title = "Source Verification",
                Content = "<h3>Source Verification Checklist</h3><ul><li>☐ All sources are credible and from reputable publications</li><li>☐ Sources are current and relevant to the topic</li><li>☐ Primary sources are used where possible</li><li>☐ All URLs and DOIs are functional and accessible</li><li>☐ Book editions and publication years are accurate</li><li>☐ Journal volume and issue numbers are correct</li><li>☐ Page numbers in citations match the actual source</li><li>☐ Avoid over-reliance on a single source</li><li>☐ Minimum source requirements are met (as specified in order details)</li></ul>",
                Category = WriterResourceCategory.WriterChecklist,
                SubCategory = "Source Checks",
                Tags = "sources, verification, credibility, references",
                SortOrder = 3,
                IsActive = true
            });

            resources.Add(new WriterResource
            {
                Title = "Grammar Review Checklist",
                Content = "<h3>Grammar and Language Checklist</h3><ul><li>☐ Spelling is checked throughout the document</li><li>☐ Subject-verb agreement is consistent</li><li>☐ Verb tenses are used correctly and consistently</li><li>☐ Pronoun references are clear and unambiguous</li><li>☐ Sentence fragments and run-on sentences are corrected</li><li>☐ Comma usage follows standard grammar rules</li><li>☐ Active voice is used where possible</li><li>☐ Technical/academic vocabulary is used correctly</li><li>☐ No contractions in formal academic writing</li><li>☐ First-person pronouns used only where appropriate for the assignment type</li></ul>",
                Category = WriterResourceCategory.WriterChecklist,
                SubCategory = "Grammar Checks",
                Tags = "grammar, spelling, punctuation, language",
                SortOrder = 4,
                IsActive = true
            });

            // ────────────────────────────────────────
            // Academic Resources
            // ────────────────────────────────────────
            resources.Add(new WriterResource
            {
                Title = "Academic Databases and Research Tools",
                Content = "<h3>Recommended Academic Resources</h3><h4>Database Access</h4><ul><li><strong>Google Scholar</strong> — scholar.google.com — Broad academic search engine</li><li><strong>JSTOR</strong> — jstor.org — Digital library of academic journals</li><li><strong>PubMed</strong> — pubmed.ncbi.nlm.nih.gov — Biomedical and life sciences literature</li><li><strong>IEEE Xplore</strong> — ieeexplore.ieee.org — Engineering and computer science</li><li><strong>ScienceDirect</strong> — sciencedirect.com — Scientific and medical research</li></ul><h4>Writing and Reference Tools</h4><ul><li><strong>Zotero</strong> — Free reference management software</li><li><strong>Mendeley</strong> — Academic reference manager and PDF organizer</li><li><strong>Grammarly</strong> — Grammar and plagiarism checking tool</li><li><strong>Hemingway Editor</strong> — Readability and style checker</li></ul>",
                Category = WriterResourceCategory.AcademicResources,
                SubCategory = "Databases & Tools",
                Tags = "database, research, tools, Google Scholar, Zotero",
                SortOrder = 1,
                IsActive = true
            });

            resources.Add(new WriterResource
            {
                Title = "Research Methodology Fundamentals",
                Content = "<h3>Research Methodology</h3><h4>Quantitative Methods</h4><ul><li>Surveys and questionnaires</li><li>Experiments and controlled studies</li><li>Statistical analysis (SPSS, R, Python)</li><li>Large-scale data analysis</li></ul><h4>Qualitative Methods</h4><ul><li>Interviews (structured, semi-structured, unstructured)</li><li>Focus groups</li><li>Case studies</li><li>Content analysis</li><li>Thematic analysis</li></ul><h4>Mixed Methods</h4><p>Combining quantitative and qualitative approaches to provide a more comprehensive understanding of the research problem.</p><h4>Ethical Considerations</h4><ul><li>Informed consent</li><li>Confidentiality and anonymity</li><li>Institutional Review Board (IRB) approval when required</li></ul>",
                Category = WriterResourceCategory.AcademicResources,
                SubCategory = "Methodology",
                Tags = "methodology, quantitative, qualitative, research design",
                SortOrder = 2,
                IsActive = true
            });

            context.WriterResources.AddRange(resources);
            await context.SaveChangesAsync();
        }
    }
}