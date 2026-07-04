namespace ScholarRescue.ViewModels.Home;

public class SpecialtyViewModel
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string SeoTitle { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty;
    public string Heading { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string HeroColor { get; set; } = "#1b365d";
    public string IconBgColor { get; set; } = "rgba(27,54,93,0.08)";
    public string IconColor { get; set; } = "#1b365d";
    public List<RelatedSpecialty> RelatedSpecialties { get; set; } = new();

    public static List<SpecialtyViewModel> GetAll()
    {
        return new List<SpecialtyViewModel>
        {
            new()
            {
                Slug = "nursing",
                Title = "Nursing Assignment Help",
                SeoTitle = "Nursing Assignment Help | Expert Nursing Academic Support | ScholarRescue",
                MetaDescription = "Professional nursing assignment help from experienced human nursing professionals. BSN, MSN, DNP support for care plans, research papers, capstone projects, and evidence-based practice. Plagiarism-free, confidential, timely delivery.",
                IconClass = "bi bi-heart-pulse-fill",
                Heading = "Nursing Assignment Help",
                HeroColor = "#1b365d",
                IconBgColor = "rgba(27,54,93,0.08)",
                IconColor = "#1b365d",
                Body = @"<p>Nursing education requires more than memorizing theories—it demands critical thinking, evidence-based practice, clinical reasoning, and professional academic writing. At ScholarRescue, we connect nursing students with experienced human nursing professionals who understand BSN, MSN, DNP, and RN-to-BSN coursework.</p>

<p>Whether you're working on care plans, nursing research papers, pathophysiology assignments, pharmacology discussions, evidence-based practice projects, or capstone papers, our experts are ready to help.</p>

<p>We also support reflective journals, clinical reasoning notes, concept maps, leadership papers, care coordination plans, and discussion responses that need clear connections between theory, patient safety, and professional nursing standards.</p>

<p>Every assignment is researched from credible scholarly sources, follows your university's formatting guidelines (APA, MLA, Harvard, Chicago, or institution-specific styles), and is tailored specifically to your instructions. We never rely on AI-generated content or recycled papers. Every document is written from scratch by a qualified human academic writer and carefully reviewed before delivery.</p>

<p>Our goal is not simply to complete your assignment but to help you submit work that reflects academic excellence while meeting deadlines with confidence. Your privacy remains completely confidential, and every paper is checked to ensure originality and plagiarism-free submission.</p>

<p>With ScholarRescue, you receive professional nursing academic support you can trust.</p>"
            },
            new()
            {
                Slug = "medicine",
                Title = "Medical Assignment Help",
                SeoTitle = "Medical Assignment Help | Expert Medical Academic Support | ScholarRescue",
                MetaDescription = "Professional medical assignment help from experienced human medical experts. Case studies, literature reviews, anatomy, pathology, pharmacology, and clinical documentation. Original, plagiarism-free, confidential support.",
                IconClass = "bi bi-activity",
                Heading = "Medical Assignment Help",
                HeroColor = "#28a745",
                IconBgColor = "rgba(40,167,69,0.08)",
                IconColor = "#28a745",
                Body = @"<p>Medical education is among the most demanding academic programs, requiring precision, analytical reasoning, and deep understanding of clinical science. ScholarRescue provides professional academic support from experienced human medical experts who understand undergraduate and postgraduate medical coursework.</p>

<p>Whether you're preparing case studies, literature reviews, anatomy assignments, pathology reports, pharmacology research, clinical documentation, epidemiology projects, or medical ethics papers, our specialists produce thoroughly researched and academically sound work.</p>

<p>We help organize complex clinical information into clear academic arguments, explain disease mechanisms, compare treatment options, and connect medical theory to practical decision-making, patient safety, and evidence-based healthcare practice.</p>

<p>Every assignment is written individually according to your professor's instructions using current peer-reviewed medical literature and evidence-based guidelines. We never submit AI-generated content or copied material. Each paper undergoes originality verification and quality review before delivery.</p>

<p>Our experts understand the importance of accuracy, proper medical terminology, and academic integrity. We also ensure proper citation formatting and logical organization that meets university expectations.</p>

<p>ScholarRescue offers dependable medical academic assistance that allows you to meet deadlines confidently while maintaining the highest academic standards and complete confidentiality.</p>"
            },
            new()
            {
                Slug = "public-health",
                Title = "Public Health Assignment Help",
                SeoTitle = "Public Health Assignment Help | Expert Public Health Academic Support | ScholarRescue",
                MetaDescription = "Professional public health assignment help from experienced human public health professionals. Epidemiology, biostatistics, health policy, community health, and global health research. Original, plagiarism-free, confidential.",
                IconClass = "bi bi-globe-americas",
                Heading = "Public Health Assignment Help",
                HeroColor = "#17a2b8",
                IconBgColor = "rgba(23,162,184,0.08)",
                IconColor = "#17a2b8",
                Body = @"<p>Public Health combines research, statistics, epidemiology, health promotion, policy analysis, and community health into one challenging discipline. ScholarRescue offers expert academic assistance from experienced public health professionals who understand the expectations of modern universities.</p>

<p>Whether you're writing epidemiology reports, health policy analyses, community assessments, biostatistics projects, environmental health papers, global health research, or health promotion proposals, our specialists deliver customized academic work that meets your course requirements.</p>

<p>We can help interpret population health data, frame research questions, compare intervention strategies, discuss social determinants of health, and translate evidence into practical recommendations for communities, agencies, and healthcare systems.</p>

<p>Every assignment is researched using credible peer-reviewed journals and authoritative public health resources. We produce completely original work written by qualified human academic experts without relying on AI-generated text or recycled papers.</p>

<p>Our writers pay close attention to evidence-based recommendations, proper academic structure, data interpretation, and citation standards while maintaining confidentiality throughout the process.</p>

<p>ScholarRescue is committed to helping students submit professional-quality assignments that demonstrate critical thinking, academic integrity, and mastery of public health concepts while remaining completely plagiarism-free.</p>"
            },
            new()
            {
                Slug = "pharmacy",
                Title = "Pharmacy Assignment Help",
                SeoTitle = "Pharmacy Assignment Help | Expert Pharmacy Academic Support | ScholarRescue",
                MetaDescription = "Professional pharmacy assignment help from experienced human pharmacy professionals. Pharmacology, medicinal chemistry, therapeutics, clinical pharmacy case studies. Original, plagiarism-free, confidential support.",
                IconClass = "bi bi-capsule",
                Heading = "Pharmacy Assignment Help",
                HeroColor = "#d4941e",
                IconBgColor = "rgba(245,166,35,0.12)",
                IconColor = "#d4941e",
                Body = @"<p>Pharmacy studies require accuracy, scientific knowledge, and attention to detail across pharmacology, medicinal chemistry, therapeutics, and clinical practice. ScholarRescue connects students with experienced human pharmacy professionals who understand university-level pharmaceutical education.</p>

<p>We provide assistance with pharmacology papers, drug calculations, pharmaceutical care plans, medicinal chemistry assignments, clinical pharmacy case studies, research projects, and evidence-based therapeutic analyses.</p>

<p>Our specialists can help explain mechanisms of action, compare treatment choices, evaluate medication safety concerns, discuss pharmacokinetics, and present patient-centered recommendations in a clear, accurate, and well-referenced academic format.</p>

<p>Every assignment is individually researched and written using current pharmaceutical literature and respected academic references. We do not use AI-generated writing or copied content. Every document is prepared from scratch and reviewed for originality before delivery.</p>

<p>Our pharmacy experts understand medication mechanisms, patient-centered care, drug interactions, and professional pharmacy standards while ensuring compliance with your university's formatting requirements.</p>

<p>Whether your assignment is simple or highly technical, ScholarRescue delivers confidential, plagiarism-free academic support designed to help you succeed with confidence.</p>"
            },
            new()
            {
                Slug = "healthcare-administration",
                Title = "Healthcare Administration Assignment Help",
                SeoTitle = "Healthcare Administration Assignment Help | Expert Healthcare Management Support | ScholarRescue",
                MetaDescription = "Professional healthcare administration assignment help from experienced human healthcare management professionals. Leadership, finance, policy, quality improvement, health informatics. Original, plagiarism-free, confidential.",
                IconClass = "bi bi-building",
                Heading = "Healthcare Administration Assignment Help",
                HeroColor = "#6c757d",
                IconBgColor = "rgba(108,117,125,0.08)",
                IconColor = "#6c757d",
                Body = @"<p>Healthcare Administration combines leadership, finance, policy, quality improvement, strategic planning, and healthcare operations. ScholarRescue provides expert academic support from professionals experienced in healthcare management and administration.</p>

<p>Our experts assist with healthcare leadership papers, quality improvement projects, healthcare finance assignments, policy analysis, strategic management reports, health informatics, organizational behavior, and healthcare ethics coursework.</p>

<p>We help evaluate operational problems, compare leadership models, analyze regulatory issues, build quality improvement arguments, and connect administrative decisions to patient outcomes, staffing, equity, compliance, and organizational performance.</p>

<p>Every assignment is carefully researched using scholarly healthcare management resources and written entirely by qualified human academic professionals. We never rely on AI-generated content or recycled assignments.</p>

<p>Our writers understand healthcare regulations, organizational leadership principles, evidence-based decision-making, and academic writing standards required by universities.</p>

<p>Each paper is customized according to your assignment instructions, properly referenced, plagiarism-free, and delivered confidentially before your deadline.</p>

<p>ScholarRescue helps future healthcare leaders achieve academic success while maintaining the highest standards of originality and professionalism.</p>"
            },
            new()
            {
                Slug = "psychology",
                Title = "Psychology Assignment Help",
                SeoTitle = "Psychology Assignment Help | Expert Psychology Academic Support | ScholarRescue",
                MetaDescription = "Professional psychology assignment help from experienced human psychology specialists. Cognitive, developmental, abnormal psychology, counseling, research papers. Original, plagiarism-free, confidential.",
                IconClass = "bi bi-brain",
                Heading = "Psychology Assignment Help",
                HeroColor = "#6f42c1",
                IconBgColor = "rgba(111,66,193,0.08)",
                IconColor = "#6f42c1",
                Body = @"<p>Psychology explores human behavior, cognition, mental health, development, and research methods. ScholarRescue offers professional academic support from experienced human psychology specialists familiar with undergraduate and graduate psychology programs.</p>

<p>We assist with cognitive psychology, developmental psychology, abnormal psychology, counseling, behavioral science, research papers, literature reviews, case studies, experimental reports, and psychological assessments.</p>

<p>Our writers can help explain theoretical perspectives, critique journal articles, organize research designs, discuss ethical considerations, and connect psychological concepts to behavior, learning, development, assessment, or mental health practice.</p>

<p>Every assignment is written from the ground up using peer-reviewed psychological research and current scholarly evidence. We never use AI-generated content or recycled papers, ensuring your work remains completely original.</p>

<p>Our experts understand psychological theories, research methodologies, statistical analysis, ethical standards, and APA formatting required in psychology education. We also keep the tone balanced, respectful, and academically appropriate.</p>

<p>With ScholarRescue, students receive personalized academic assistance that emphasizes quality research, confidentiality, plagiarism-free writing, and timely delivery for every psychology assignment.</p>"
            },
            new()
            {
                Slug = "nutrition",
                Title = "Nutrition Assignment Help",
                SeoTitle = "Nutrition Assignment Help | Expert Nutrition Academic Support | ScholarRescue",
                MetaDescription = "Professional nutrition assignment help from qualified human nutrition professionals. Nutritional assessments, diet planning, clinical nutrition, metabolism research. Original, plagiarism-free, confidential.",
                IconClass = "bi bi-apple",
                Heading = "Nutrition Assignment Help",
                HeroColor = "#dc3545",
                IconBgColor = "rgba(220,53,69,0.08)",
                IconColor = "#dc3545",
                Body = @"<p>Nutrition science requires understanding human physiology, metabolism, food science, clinical nutrition, and evidence-based dietary recommendations. ScholarRescue connects students with qualified human nutrition professionals who produce academically rigorous assignments tailored to university standards.</p>

<p>Our experts assist with nutritional assessments, diet planning, community nutrition, clinical nutrition case studies, metabolism research, food science projects, public health nutrition, and evidence-based nutrition interventions.</p>

<p>We can help analyze dietary patterns, explain nutrient functions, compare intervention approaches, interpret assessment data, and connect recommendations to culture, disease risk, patient goals, and evidence-based nutrition practice.</p>

<p>Every paper is written from scratch using current peer-reviewed nutritional research and trusted scientific resources. We never use AI-generated writing or duplicate existing work.</p>

<p>Assignments are carefully structured, properly referenced, plagiarism-free, and customized to your professor's instructions. Our specialists understand current dietary guidelines, nutritional therapy, and academic research expectations.</p>

<p>ScholarRescue provides confidential, reliable academic support that helps nutrition students submit original, professionally written coursework with complete confidence.</p>"
            },
            new()
            {
                Slug = "occupational-therapy",
                Title = "Occupational Therapy Assignment Help",
                SeoTitle = "Occupational Therapy Assignment Help | Expert OT Academic Support | ScholarRescue",
                MetaDescription = "Professional occupational therapy assignment help from experienced human OT professionals. Case studies, intervention plans, rehabilitation research, pediatric therapy. Original, plagiarism-free, confidential.",
                IconClass = "bi bi-arrow-repeat",
                Heading = "Occupational Therapy Assignment Help",
                HeroColor = "#009688",
                IconBgColor = "rgba(0,150,136,0.08)",
                IconColor = "#009688",
                Body = @"<p>Occupational Therapy requires students to integrate rehabilitation science, anatomy, patient-centered care, therapeutic interventions, and evidence-based practice. ScholarRescue provides expert academic assistance from experienced human professionals familiar with OT education.</p>

<p>We help with occupational therapy case studies, intervention plans, rehabilitation research, pediatric therapy assignments, mental health practice papers, assistive technology projects, and evidence-based clinical documentation.</p>

<p>Our specialists can connect occupational performance, client goals, environmental barriers, adaptive strategies, functional assessments, and therapeutic outcomes in writing that is clinically thoughtful and academically organized.</p>

<p>Every assignment is developed individually using current scholarly research and professional occupational therapy literature. We never generate assignments using AI or reuse previous papers.</p>

<p>Our writers understand occupational therapy frameworks, clinical reasoning, functional assessments, and patient-centered intervention planning while following university citation requirements.</p>

<p>Every document is confidential, plagiarism-free, and thoroughly reviewed for academic quality before delivery.</p>

<p>ScholarRescue helps occupational therapy students produce high-quality coursework that reflects professionalism, originality, and academic excellence.</p>"
            },
            new()
            {
                Slug = "physical-therapy",
                Title = "Physical Therapy Assignment Help",
                SeoTitle = "Physical Therapy Assignment Help | Expert PT Academic Support | ScholarRescue",
                MetaDescription = "Professional physical therapy assignment help from experienced human PT professionals. Rehabilitation plans, musculoskeletal case studies, biomechanics research. Original, plagiarism-free, confidential.",
                IconClass = "bi bi-person-arms-up",
                Heading = "Physical Therapy Assignment Help",
                HeroColor = "#007bff",
                IconBgColor = "rgba(0,123,255,0.08)",
                IconColor = "#007bff",
                Body = @"<p>Physical Therapy education demands a thorough understanding of anatomy, biomechanics, rehabilitation, movement science, and evidence-based patient care. ScholarRescue provides academic support from experienced human physical therapy professionals.</p>

<p>We assist with rehabilitation plans, musculoskeletal case studies, neurological rehabilitation, exercise prescription, biomechanics research, patient assessment reports, clinical documentation, and PT research projects.</p>

<p>We help explain movement impairments, compare intervention approaches, justify exercise progressions, discuss outcome measures, and connect clinical evidence to safe, patient-centered rehabilitation planning and professional documentation.</p>

<p>Each assignment is researched using peer-reviewed physical therapy literature and written exclusively by qualified human experts. We never rely on AI-generated writing or copied content.</p>

<p>Our specialists ensure proper clinical terminology, evidence-based recommendations, logical organization, and adherence to university formatting requirements. We also keep clinical reasoning clear, measurable, and easy to follow.</p>

<p>Every assignment is confidential, original, and plagiarism-free, helping students confidently submit professional-quality academic work.</p>

<p>ScholarRescue is committed to supporting future physical therapists through reliable, high-quality academic assistance.</p>"
            },
            new()
            {
                Slug = "midwifery",
                Title = "Midwifery Assignment Help",
                SeoTitle = "Midwifery Assignment Help | Expert Midwifery Academic Support | ScholarRescue",
                MetaDescription = "Professional midwifery assignment help from experienced human midwifery specialists. Maternal health, antenatal care, labor and delivery, neonatal care. Original, plagiarism-free, confidential.",
                IconClass = "bi bi-flower1",
                Heading = "Midwifery Assignment Help",
                HeroColor = "#e91e63",
                IconBgColor = "rgba(233,30,99,0.08)",
                IconColor = "#e91e63",
                Body = @"<p>Midwifery education requires expertise in maternal health, prenatal care, childbirth, neonatal care, reproductive health, and evidence-based clinical practice. ScholarRescue provides professional academic support from experienced human midwifery specialists.</p>

<p>We assist with maternal health research, antenatal care assignments, labor and delivery case studies, neonatal care papers, women's health projects, evidence-based practice assignments, and reflective clinical documentation.</p>

<p>Our experts can help discuss risk assessment, culturally responsive care, patient education, complications, continuity of care, and the evidence behind safe maternal, reproductive, and newborn health decisions.</p>

<p>Every assignment is written from scratch using current scholarly healthcare literature and trusted professional guidelines. We never use AI-generated content or recycled papers.</p>

<p>Our experts understand maternity care standards, patient-centered practice, ethical considerations, and academic writing expectations required by universities. We also write with sensitivity to family-centered care and professional accountability.</p>

<p>Each paper is confidential, professionally referenced, plagiarism-free, and delivered on time to help students achieve academic excellence.</p>

<p>ScholarRescue provides dependable academic assistance designed specifically for future midwives.</p>"
            },
            new()
            {
                Slug = "medical-laboratory-science",
                Title = "Medical Laboratory Science Assignment Help",
                SeoTitle = "Medical Laboratory Science Assignment Help | Expert MLS Academic Support | ScholarRescue",
                MetaDescription = "Professional medical laboratory science assignment help from experienced human lab science professionals. Microbiology, hematology, immunology, pathology, diagnostics. Original, plagiarism-free, confidential.",
                IconClass = "bi bi-microscope",
                Heading = "Medical Laboratory Science Assignment Help",
                HeroColor = "#ff5722",
                IconBgColor = "rgba(255,87,34,0.08)",
                IconColor = "#ff5722",
                Body = @"<p>Medical Laboratory Science combines laboratory diagnostics, microbiology, hematology, immunology, pathology, and clinical analysis. ScholarRescue provides expert academic assistance from experienced human laboratory science professionals.</p>

<p>We help students complete laboratory reports, microbiology assignments, hematology case studies, pathology research, immunology papers, diagnostic analysis, laboratory quality assurance projects, and clinical investigations.</p>

<p>We can help explain testing principles, compare diagnostic methods, interpret laboratory findings, discuss quality control, and present technical scientific information in a clear academic structure.</p>

<p>Every assignment is individually researched using credible scientific journals and professional laboratory standards. We never generate assignments using AI or submit recycled work.</p>

<p>Our specialists understand laboratory procedures, diagnostic interpretation, research methodology, and academic formatting requirements expected in medical laboratory education. We also keep technical explanations precise, organized, and easy to verify.</p>

<p>Every document is completely original, plagiarism-free, confidential, and quality-reviewed before delivery.</p>

<p>ScholarRescue helps laboratory science students submit academically outstanding assignments while maintaining the highest standards of integrity and professionalism.</p>"
            },
            new()
            {
                Slug = "radiography",
                Title = "Radiography Assignment Help",
                SeoTitle = "Radiography Assignment Help | Expert Radiography Academic Support | ScholarRescue",
                MetaDescription = "Professional radiography assignment help from experienced human radiography specialists. Diagnostic imaging, radiation safety, CT and MRI case studies. Original, plagiarism-free, confidential.",
                IconClass = "bi bi-x-ray",
                Heading = "Radiography Assignment Help",
                HeroColor = "#3f51b5",
                IconBgColor = "rgba(63,81,181,0.08)",
                IconColor = "#3f51b5",
                Body = @"<p>Radiography requires students to master medical imaging, anatomy, radiation safety, diagnostic procedures, patient care, and clinical interpretation. ScholarRescue provides professional academic support from experienced human radiography specialists.</p>

<p>Our experts assist with diagnostic imaging assignments, radiologic science research, radiation protection papers, CT and MRI case studies, imaging technology projects, patient care documentation, and evidence-based radiography coursework.</p>

<p>We can help explain image acquisition, compare imaging modalities, discuss patient positioning, evaluate safety practices, and connect technical decisions to diagnostic quality, ethical practice, and patient-centered care.</p>

<p>Every assignment is written from scratch using current scholarly references and professional imaging standards. We never rely on AI-generated writing or copied content.</p>

<p>Our writers understand imaging protocols, radiation safety regulations, diagnostic accuracy, ethical practice, and university academic expectations.</p>

<p>Each paper is fully customized to your assignment instructions, properly referenced, plagiarism-free, and delivered confidentially before your deadline.</p>

<p>With ScholarRescue, radiography students receive reliable academic support that emphasizes originality, professional quality, and academic success.</p>"
            },
            new()
            {
                Slug = "dentistry",
                Title = "Dentistry Assignment Help",
                SeoTitle = "Dentistry Assignment Help | Expert Dentistry Academic Support | ScholarRescue",
                MetaDescription = "Professional dentistry assignment help from experienced human dental professionals. Clinical documentation, research papers, evidence-based practice, and dental coursework support. Original, plagiarism-free, confidential.",
                IconClass = "bi bi-tooth",
                Heading = "Dentistry Assignment Help",
                HeroColor = "#1b365d",
                IconBgColor = "rgba(27,54,93,0.08)",
                IconColor = "#1b365d",
                Body = @"<p>Dentistry education demands precision, clinical expertise, and a deep understanding of oral health sciences, dental anatomy, and evidence-based patient care. ScholarRescue connects dental students with experienced human dental professionals who understand the rigors of dental school coursework.</p>

<p>Whether you need assistance with clinical case studies, dental research papers, oral pathology assignments, periodontology projects, orthodontic case analyses, prosthodontics documentation, or evidence-based dentistry reviews, our specialists deliver thoroughly researched and professionally written academic work.</p>

<p>We can help explain treatment planning, compare preventive strategies, discuss oral-systemic health links, analyze patient cases, and organize technical dental concepts into clear academic writing.</p>

<p>Every assignment is written from scratch using current peer-reviewed dental literature and respected clinical references. We never use AI-generated content or recycled papers. Each document is prepared by a qualified human academic writer and carefully reviewed for originality and quality before delivery.</p>

<p>Our experts understand dental terminology, clinical documentation standards, proper citation formatting, and the academic expectations of modern dental programs. Your privacy remains completely confidential throughout the process.</p>

<p>ScholarRescue provides dependable dental academic support that helps you meet deadlines with confidence while maintaining the highest standards of academic integrity and professionalism.</p>"
            }
        };
    }

    public static SpecialtyViewModel? GetBySlug(string slug)
    {
        return GetAll().FirstOrDefault(s =>
            s.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));
    }
}

public class RelatedSpecialty
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty;
    public string IconBgColor { get; set; } = "rgba(27,54,93,0.08)";
    public string IconColor { get; set; } = "#1b365d";
}
