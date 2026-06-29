#!/bin/bash
set -e

cd /var/www/scholarrescue

# Create directories if needed
mkdir -p Views/Home Views/Shared wwwroot/css

# Deploy _AnnouncementTicker.cshtml
cat > Views/Shared/_AnnouncementTicker.cshtml << 'TICKEREOF'
@* ScholarRescue - Reusable Announcement Ticker Component *@
<div class="ticker-wrapper" aria-label="Platform announcements" role="marquee">
    <div class="ticker-track">
        <div class="ticker-content">
            <span class="ticker-item"><i class="bi bi-heart-pulse-fill ticker-icon"></i>Human Expert Academic Support</span>
            <span class="ticker-sep" aria-hidden="true">&bull;</span>
            <span class="ticker-item"><i class="bi bi-mortarboard-fill ticker-icon"></i>Built for Healthcare and Nursing Students</span>
            <span class="ticker-sep" aria-hidden="true">&bull;</span>
            <span class="ticker-item"><i class="bi bi-journal-medical ticker-icon"></i>Comprehensive Clinical and Academic Support</span>
            <span class="ticker-sep" aria-hidden="true">&bull;</span>
            <span class="ticker-item"><i class="bi bi-shield-check ticker-icon"></i>Quality, Confidentiality, and Academic Integrity</span>
            <span class="ticker-sep" aria-hidden="true">&bull;</span>
            <span class="ticker-item"><i class="bi bi-globe2 ticker-icon"></i>Supporting Students Worldwide</span>
        </div>
        <div class="ticker-content" aria-hidden="true">
            <span class="ticker-item"><i class="bi bi-heart-pulse-fill ticker-icon"></i>Human Expert Academic Support</span>
            <span class="ticker-sep" aria-hidden="true">&bull;</span>
            <span class="ticker-item"><i class="bi bi-mortarboard-fill ticker-icon"></i>Built for Healthcare and Nursing Students</span>
            <span class="ticker-sep" aria-hidden="true">&bull;</span>
            <span class="ticker-item"><i class="bi bi-journal-medical ticker-icon"></i>Comprehensive Clinical and Academic Support</span>
            <span class="ticker-sep" aria-hidden="true">&bull;</span>
            <span class="ticker-item"><i class="bi bi-shield-check ticker-icon"></i>Quality, Confidentiality, and Academic Integrity</span>
            <span class="ticker-sep" aria-hidden="true">&bull;</span>
            <span class="ticker-item"><i class="bi bi-globe2 ticker-icon"></i>Supporting Students Worldwide</span>
        </div>
    </div>
</div>
TICKEREOF
echo "Deployed _AnnouncementTicker.cshtml"

# Deploy Index.cshtml
cat > Views/Home/Index.cshtml << 'INDEXEOF'
@{
    ViewData["Title"] = "Home";
    Layout = "~/Views/Shared/_Layout.Public.cshtml";
}

<!-- Premium Announcement Ticker -->
@await Html.PartialAsync("_AnnouncementTicker")

<!-- Hero Section -->
<section class="hero-section">
    <div class="container">
        <div class="row align-items-center min-vh-50">
            <div class="col-lg-6">
                <h1 class="display-5 fw-bold text-white mb-3">Expert Academic Support for Healthcare, Nursing and University Students</h1>
                <p class="lead text-white-50 mb-4">
                    Connect with experienced academic professionals for personalized assistance with coursework, research, clinical documentation, evidence-based practice, tutoring, editing, proofreading, and academic success throughout your educational journey.
                </p>
                <div class="d-flex gap-3 flex-wrap">
                    <a asp-controller="Home" asp-action="OrderNow" class="btn btn-accent btn-lg px-4">
                        <i class="bi bi-lightning-charge-fill me-2"></i>Order Now
                    </a>
                    <a asp-controller="Home" asp-action="BecomeATutor" class="btn btn-outline-light btn-lg px-4">
                        <i class="bi bi-person-plus me-2"></i>Become a Tutor
                    </a>
                </div>
            </div>
            <div class="col-lg-6 d-none d-lg-block text-center">
                <div class="hero-illustration">
                    <i class="bi bi-mortarboard" style="font-size:8rem;color:rgba(255,255,255,0.15);"></i>
                    <i class="bi bi-book" style="font-size:4rem;color:rgba(245,166,35,0.3);position:absolute;top:20%;right:20%;"></i>
                    <i class="bi bi-pencil-square" style="font-size:3rem;color:rgba(255,255,255,0.2);position:absolute;bottom:20%;left:20%;"></i>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Healthcare and Nursing Specialists Section -->
<section class="py-5">
    <div class="container">
        <div class="text-center mb-5">
            <h2 class="fw-bold">Healthcare and Nursing Specialists</h2>
            <p class="text-muted">Specialized academic support for healthcare education, clinical learning, research, and professional development.</p>
        </div>
        <div class="row g-3">
            <div class="col-lg-3 col-md-4 col-sm-6">
                <div class="card h-100 border-0 shadow-sm specialty-card text-center p-3">
                    <div class="card-body">
                        <div class="rounded-circle bg-primary bg-opacity-10 d-inline-flex align-items-center justify-content-center mb-2" style="width:48px;height:48px;">
                            <i class="bi bi-heart-pulse-fill fs-4 text-primary"></i>
                        </div>
                        <h6 class="fw-bold mb-1">Nursing</h6>
                        <p class="text-muted small mb-2">BSN, MSN, DNP, NCLEX prep, care plans, clinical reflections, and evidence-based practice.</p>
                        <a asp-controller="Home" asp-action="Tutoring" class="btn btn-outline-primary btn-sm">Learn More <i class="bi bi-arrow-right"></i></a>
                    </div>
                </div>
            </div>
            <div class="col-lg-3 col-md-4 col-sm-6">
                <div class="card h-100 border-0 shadow-sm specialty-card text-center p-3">
                    <div class="card-body">
                        <div class="rounded-circle bg-danger bg-opacity-10 d-inline-flex align-items-center justify-content-center mb-2" style="width:48px;height:48px;">
                            <i class="bi bi-activity fs-4 text-danger"></i>
                        </div>
                        <h6 class="fw-bold mb-1">Medicine</h6>
                        <p class="text-muted small mb-2">Research papers, case studies, clinical documentation, and academic writing for medical students.</p>
                        <a asp-controller="Home" asp-action="ResearchGuidance" class="btn btn-outline-primary btn-sm">Learn More <i class="bi bi-arrow-right"></i></a>
                    </div>
                </div>
            </div>
            <div class="col-lg-3 col-md-4 col-sm-6">
                <div class="card h-100 border-0 shadow-sm specialty-card text-center p-3">
                    <div class="card-body">
                        <div class="rounded-circle bg-success bg-opacity-10 d-inline-flex align-items-center justify-content-center mb-2" style="width:48px;height:48px;">
                            <i class="bi bi-globe-americas fs-4 text-success"></i>
                        </div>
                        <h6 class="fw-bold mb-1">Public Health</h6>
                        <p class="text-muted small mb-2">Epidemiology, biostatistics, health policy, community health, and global health research.</p>
                        <a asp-controller="Home" asp-action="ResearchGuidance" class="btn btn-outline-primary btn-sm">Learn More <i class="bi bi-arrow-right"></i></a>
                    </div>
                </div>
            </div>
            <div class="col-lg-3 col-md-4 col-sm-6">
                <div class="card h-100 border-0 shadow-sm specialty-card text-center p-3">
                    <div class="card-body">
                        <div class="rounded-circle bg-info bg-opacity-10 d-inline-flex align-items-center justify-content-center mb-2" style="width:48px;height:48px;">
                            <i class="bi bi-capsule fs-4 text-info"></i>
                        </div>
                        <h6 class="fw-bold mb-1">Pharmacy</h6>
                        <p class="text-muted small mb-2">PharmD coursework, pharmacology, drug literature reviews, and clinical pharmacy assignments.</p>
                        <a asp-controller="Home" asp-action="Tutoring" class="btn btn-outline-primary btn-sm">Learn More <i class="bi bi-arrow-right"></i></a>
                    </div>
                </div>
            </div>
            <div class="col-lg-3 col-md-4 col-sm-6">
                <div class="card h-100 border-0 shadow-sm specialty-card text-center p-3">
                    <div class="card-body">
                        <div class="rounded-circle bg-warning bg-opacity-10 d-inline-flex align-items-center justify-content-center mb-2" style="width:48px;height:48px;">
                            <i class="bi bi-building fs-4 text-warning"></i>
                        </div>
                        <h6 class="fw-bold mb-1">Healthcare Administration</h6>
                        <p class="text-muted small mb-2">Health management, policy analysis, healthcare finance, and organizational leadership.</p>
                        <a asp-controller="Home" asp-action="Tutoring" class="btn btn-outline-primary btn-sm">Learn More <i class="bi bi-arrow-right"></i></a>
                    </div>
                </div>
            </div>
            <div class="col-lg-3 col-md-4 col-sm-6">
                <div class="card h-100 border-0 shadow-sm specialty-card text-center p-3">
                    <div class="card-body">
                        <div class="rounded-circle bg-purple bg-opacity-10 d-inline-flex align-items-center justify-content-center mb-2" style="width:48px;height:48px;">
                            <i class="bi bi-brain fs-4" style="color:#6f42c1;"></i>
                        </div>
                        <h6 class="fw-bold mb-1">Psychology</h6>
                        <p class="text-muted small mb-2">Clinical, counseling, developmental psychology, research methods, and APA-style writing.</p>
                        <a asp-controller="Home" asp-action="Tutoring" class="btn btn-outline-primary btn-sm">Learn More <i class="bi bi-arrow-right"></i></a>
                    </div>
                </div>
            </div>
            <div class="col-lg-3 col-md-4 col-sm-6">
                <div class="card h-100 border-0 shadow-sm specialty-card text-center p-3">
                    <div class="card-body">
                        <div class="rounded-circle bg-success bg-opacity-10 d-inline-flex align-items-center justify-content-center mb-2" style="width:48px;height:48px;">
                            <i class="bi bi-flower1 fs-4 text-success"></i>
                        </div>
                        <h6 class="fw-bold mb-1">Nutrition</h6>
                        <p class="text-muted small mb-2">Dietetics, clinical nutrition, nutritional science, and dietetic internship documentation.</p>
                        <a asp-controller="Home" asp-action="Tutoring" class="btn btn-outline-primary btn-sm">Learn More <i class="bi bi-arrow-right"></i></a>
                    </div>
                </div>
            </div>
            <div class="col-lg-3 col-md-4 col-sm-6">
                <div class="card h-100 border-0 shadow-sm specialty-card text-center p-3">
                    <div class="card-body">
                        <div class="rounded-circle bg-info bg-opacity-10 d-inline-flex align-items-center justify-content-center mb-2" style="width:48px;height:48px;">
                            <i class="bi bi-people fs-4 text-info"></i>
                        </div>
                        <h6 class="fw-bold mb-1">Occupational Therapy</h6>
                        <p class="text-muted small mb-2">OT coursework, fieldwork documentation, clinical reasoning papers, and intervention plans.</p>
                        <a asp-controller="Home" asp-action="Tutoring" class="btn btn-outline-primary btn-sm">Learn More <i class="bi bi-arrow-right"></i></a>
                    </div>
                </div>
            </div>
            <div class="col-lg-3 col-md-4 col-sm-6">
                <div class="card h-100 border-0 shadow-sm specialty-card text-center p-3">
                    <div class="card-body">
                        <div class="rounded-circle bg-danger bg-opacity-10 d-inline-flex align-items-center justify-content-center mb-2" style="width:48px;height:48px;">
                            <i class="bi bi-person-walking fs-4 text-danger"></i>
                        </div>
                        <h6 class="fw-bold mb-1">Physical Therapy</h6>
                        <p class="text-muted small mb-2">DPT coursework, clinical case studies, evidence-based practice, and rehabilitation research.</p>
                        <a asp-controller="Home" asp-action="Tutoring" class="btn btn-outline-primary btn-sm">Learn More <i class="bi bi-arrow-right"></i></a>
                    </div>
                </div>
            </div>
            <div class="col-lg-3 col-md-4 col-sm-6">
                <div class="card h-100 border-0 shadow-sm specialty-card text-center p-3">
                    <div class="card-body">
                        <div class="rounded-circle bg-primary bg-opacity-10 d-inline-flex align-items-center justify-content-center mb-2" style="width:48px;height:48px;">
                            <i class="bi bi-heart fs-4 text-primary"></i>
                        </div>
                        <h6 class="fw-bold mb-1">Midwifery</h6>
                        <p class="text-muted small mb-2">Midwifery coursework, clinical documentation, maternal-child health research, and care plans.</p>
                        <a asp-controller="Home" asp-action="Tutoring" class="btn btn-outline-primary btn-sm">Learn More <i class="bi bi-arrow-right"></i></a>
                    </div>
                </div>
            </div>
            <div class="col-lg-3 col-md-4 col-sm-6">
                <div class="card h-100 border-0 shadow-sm specialty-card text-center p-3">
                    <div class="card-body">
                        <div class="rounded-circle bg-secondary bg-opacity-10 d-inline-flex align-items-center justify-content-center mb-2" style="width:48px;height:48px;">
                            <i class="bi bi-microscope fs-4 text-secondary"></i>
                        </div>
                        <h6 class="fw-bold mb-1">Medical Laboratory Science</h6>
                        <p class="text-muted small mb-2">Clinical lab science, hematology, microbiology, immunology, and lab management.</p>
                        <a asp-controller="Home" asp-action="Tutoring" class="btn btn-outline-primary btn-sm">Learn More <i class="bi bi-arrow-right"></i></a>
                    </div>
                </div>
            </div>
            <div class="col-lg-3 col-md-4 col-sm-6">
                <div class="card h-100 border-0 shadow-sm specialty-card text-center p-3">
                    <div class="card-body">
                        <div class="rounded-circle bg-warning bg-opacity-10 d-inline-flex align-items-center justify-content-center mb-2" style="width:48px;height:48px;">
                            <i class="bi bi-x-ray fs-4 text-warning"></i>
                        </div>
                        <h6 class="fw-bold mb-1">Radiography</h6>
                        <p class="text-muted small mb-2">Radiologic science, imaging techniques, radiation safety, and clinical case studies.</p>
                        <a asp-controller="Home" asp-action="Tutoring" class="btn btn-outline-primary btn-sm">Learn More <i class="bi bi-arrow-right"></i></a>
                    </div>
                </div>
            </div>
            <div class="col-lg-3 col-md-4 col-sm-6">
                <div class="card h-100 border-0 shadow-sm specialty-card text-center p-3">
                    <div class="card-body">
                        <div class="rounded-circle bg-info bg-opacity-10 d-inline-flex align-items-center justify-content-center mb-2" style="width:48px;height:48px;">
                            <i class="bi bi-tooth fs-4 text-info"></i>
                        </div>
                        <h6 class="fw-bold mb-1">Dentistry</h6>
                        <p class="text-muted small mb-2">Dental coursework, research papers, case reports, and clinical documentation for dental students.</p>
                        <a asp-controller="Home" asp-action="Tutoring" class="btn btn-outline-primary btn-sm">Learn More <i class="bi bi-arrow-right"></i></a>
                    </div>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Services Section -->
<section class="py-5" style="background:var(--bg-main);">
    <div class="container">
        <div class="text-center mb-5">
            <h2 class="fw-bold">Our Services</h2>
            <p class="text-muted">Comprehensive academic support tailored to your needs</p>
        </div>
        <div class="row g-4">
            <div class="col-lg-4 col-md-6">
                <div class="card h-100 border-0 shadow-sm service-card text-center p-4">
                    <div class="card-body">
                        <div class="rounded-circle bg-primary bg-opacity-10 d-inline-flex align-items-center justify-content-center mb-3" style="width:64px;height:64px;">
                            <i class="bi bi-person-workspace fs-3 text-primary"></i>
                        </div>
                        <h5 class="fw-bold">Tutoring</h5>
                        <p class="text-muted small">One-on-one tutoring sessions with qualified professionals across various academic subjects.</p>
                        <a asp-controller="Home" asp-action="Tutoring" class="btn btn-outline-primary btn-sm">Learn More <i class="bi bi-arrow-right"></i></a>
                    </div>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="card h-100 border-0 shadow-sm service-card text-center p-4">
                    <div class="card-body">
                        <div class="rounded-circle bg-success bg-opacity-10 d-inline-flex align-items-center justify-content-center mb-3" style="width:64px;height:64px;">
                            <i class="bi bi-search fs-3 text-success"></i>
                        </div>
                        <h5 class="fw-bold">Research Guidance</h5>
                        <p class="text-muted small">Expert guidance through every stage of your research process, from proposal to completion.</p>
                        <a asp-controller="Home" asp-action="ResearchGuidance" class="btn btn-outline-success btn-sm">Learn More <i class="bi bi-arrow-right"></i></a>
                    </div>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="card h-100 border-0 shadow-sm service-card text-center p-4">
                    <div class="card-body">
                        <div class="rounded-circle bg-info bg-opacity-10 d-inline-flex align-items-center justify-content-center mb-3" style="width:64px;height:64px;">
                            <i class="bi bi-pencil fs-3 text-info"></i>
                        </div>
                        <h5 class="fw-bold">Editing</h5>
                        <p class="text-muted small">Professional editing services to refine your academic papers and scholarly documents.</p>
                        <a asp-controller="Home" asp-action="Editing" class="btn btn-outline-info btn-sm">Learn More <i class="bi bi-arrow-right"></i></a>
                    </div>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="card h-100 border-0 shadow-sm service-card text-center p-4">
                    <div class="card-body">
                        <div class="rounded-circle bg-warning bg-opacity-10 d-inline-flex align-items-center justify-content-center mb-3" style="width:64px;height:64px;">
                            <i class="bi bi-check2-circle fs-3 text-warning"></i>
                        </div>
                        <h5 class="fw-bold">Proofreading</h5>
                        <p class="text-muted small">Thorough proofreading to eliminate errors and ensure your work meets the highest standards.</p>
                        <a asp-controller="Home" asp-action="Proofreading" class="btn btn-outline-warning btn-sm">Learn More <i class="bi bi-arrow-right"></i></a>
                    </div>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="card h-100 border-0 shadow-sm service-card text-center p-4">
                    <div class="card-body">
                        <div class="rounded-circle bg-danger bg-opacity-10 d-inline-flex align-items-center justify-content-center mb-3" style="width:64px;height:64px;">
                            <i class="bi bi-book fs-3 text-danger"></i>
                        </div>
                        <h5 class="fw-bold">Citation Assistance</h5>
                        <p class="text-muted small">Expert help with citations and references in APA, MLA, Chicago, Harvard and more.</p>
                        <a asp-controller="Home" asp-action="CitationAssistance" class="btn btn-outline-danger btn-sm">Learn More <i class="bi bi-arrow-right"></i></a>
                    </div>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="card h-100 border-0 shadow-sm service-card text-center p-4">
                    <div class="card-body">
                        <div class="rounded-circle bg-secondary bg-opacity-10 d-inline-flex align-items-center justify-content-center mb-3" style="width:64px;height:64px;">
                            <i class="bi bi-layout-text-window fs-3 text-secondary"></i>
                        </div>
                        <h5 class="fw-bold">Formatting Assistance</h5>
                        <p class="text-muted small">Ensure your papers meet institutional formatting guidelines and academic standards.</p>
                        <a asp-controller="Home" asp-action="FormattingAssistance" class="btn btn-outline-secondary btn-sm">Learn More <i class="bi bi-arrow-right"></i></a>
                    </div>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="card h-100 border-0 shadow-sm service-card text-center p-4">
                    <div class="card-body">
                        <div class="rounded-circle bg-primary bg-opacity-10 d-inline-flex align-items-center justify-content-center mb-3" style="width:64px;height:64px;">
                            <i class="bi bi-lightbulb fs-3 text-primary"></i>
                        </div>
                        <h5 class="fw-bold">Academic Coaching</h5>
                        <p class="text-muted small">Personalized academic coaching to develop study skills and achieve your educational goals.</p>
                        <a asp-controller="Home" asp-action="About" class="btn btn-outline-primary btn-sm">Learn More <i class="bi bi-arrow-right"></i></a>
                    </div>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Statistics Section -->
<section class="py-5 bg-white">
    <div class="container">
        <div class="row g-3 text-center">
            <div class="col-6 col-md-3">
                <div class="p-3">
                    <div class="display-5 fw-bold text-primary">5,000+</div>
                    <div class="text-muted small">Trusted Students</div>
                </div>
            </div>
            <div class="col-6 col-md-3">
                <div class="p-3">
                    <div class="display-5 fw-bold text-primary">98%</div>
                    <div class="text-muted small">Satisfaction Rate</div>
                </div>
            </div>
            <div class="col-6 col-md-3">
                <div class="p-3">
                    <div class="display-5 fw-bold text-primary">250+</div>
                    <div class="text-muted small">Qualified Writers</div>
                </div>
            </div>
            <div class="col-6 col-md-3">
                <div class="p-3">
                    <div class="display-5 fw-bold text-primary">50+</div>
                    <div class="text-muted small">Academic Fields</div>
                </div>
            </div>
        </div>
    </div>
</section>

<!-- Why Choose ScholarRescue -->
<section class="py-5">
    <div class="container">
        <div class="text-center mb-5">
            <h2 class="fw-bold">Why Choose ScholarRescue</h2>
            <p class="text-muted">Trusted by thousands of students worldwide</p>
        </div>
        <div class="row g-4">
            <div class="col-lg-4 col-md-6">
                <div class="d-flex align-items-start gap-3">
                    <div class="rounded-circle bg-primary bg-opacity-10 d-flex align-items-center justify-content-center flex-shrink-0" style="width:48px;height:48px;">
                        <i class="bi bi-patch-check-fill text-primary fs-5"></i>
                    </div>
                    <div>
                        <h6 class="fw-bold mb-1">Verified Tutors</h6>
                        <p class="text-muted small mb-0">All tutors undergo rigorous verification of qualifications and expertise.</p>
                    </div>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="d-flex align-items-start gap-3">
                    <div class="rounded-circle bg-success bg-opacity-10 d-flex align-items-center justify-content-center flex-shrink-0" style="width:48px;height:48px;">
                        <i class="bi bi-chat-dots-fill text-success fs-5"></i>
                    </div>
                    <div>
                        <h6 class="fw-bold mb-1">Secure Messaging</h6>
                        <p class="text-muted small mb-0">Communicate safely through our encrypted messaging system.</p>
                    </div>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="d-flex align-items-start gap-3">
                    <div class="rounded-circle bg-info bg-opacity-10 d-flex align-items-center justify-content-center flex-shrink-0" style="width:48px;height:48px;">
                        <i class="bi bi-tag-fill text-info fs-5"></i>
                    </div>
                    <div>
                        <h6 class="fw-bold mb-1">Transparent Pricing</h6>
                        <p class="text-muted small mb-0">Clear, upfront pricing with no hidden fees or surprise charges.</p>
                    </div>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="d-flex align-items-start gap-3">
                    <div class="rounded-circle bg-warning bg-opacity-10 d-flex align-items-center justify-content-center flex-shrink-0" style="width:48px;height:48px;">
                        <i class="bi bi-shield-lock-fill text-warning fs-5"></i>
                    </div>
                    <div>
                        <h6 class="fw-bold mb-1">Protected Payments</h6>
                        <p class="text-muted small mb-0">Your payments are held securely until you're satisfied with the work.</p>
                    </div>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="d-flex align-items-start gap-3">
                    <div class="rounded-circle bg-danger bg-opacity-10 d-flex align-items-center justify-content-center flex-shrink-0" style="width:48px;height:48px;">
                        <i class="bi bi-graph-up-arrow text-danger fs-5"></i>
                    </div>
                    <div>
                        <h6 class="fw-bold mb-1">Progress Tracking</h6>
                        <p class="text-muted small mb-0">Monitor the progress of your orders in real-time through your dashboard.</p>
                    </div>
                </div>
            </div>
            <div class="col-lg-4 col-md-6">
                <div class="d-flex align-items-start gap-3">
                    <div class="rounded-circle bg-primary bg-opacity-10 d-flex align-items-center justify-content-center flex-shrink-0" style="width:48px;height:48px;">
                        <i class="bi bi-award-fill text-primary fs-5"></i>
                    </div>
                    <div>
                        <h6 class="fw-bold mb-1">Quality Assurance</h6>
                        <p class="text-muted small mb-0">Every order is reviewed by our quality team to ensure the highest academic standards.</p>
                    </div>
                </div>
            </div>
        </div>
        <div class="text-center mt-4">
            <a asp-controller="Home" asp-action="FAQ" class="btn btn-primary">View All FAQs <i class="bi bi-arrow-right ms-1"></i></a>
        </div>
    </div>
</section>

<!-- CTA Section -->
<section class="py-5 cta-section">
    <div class="container text-center">
        <h2 class="fw-bold text-white mb-3">Need Academic Support?</h2>
        <p class="text-white-50 mb-4">Join thousands of students who trust ScholarRescue for their academic success.</p>
        <a asp-controller="Account" asp-action="Register" class="btn btn-accent btn-lg px-5">
            <i class="bi bi-rocket-takeoff me-2"></i>Create Account
        </a>
    </div>
</section>

<style>
    .hero-section {
        background: linear-gradient(135deg, var(--primary) 0%, var(--secondary) 100%);
        padding: 5rem 0;
        position: relative;
        overflow: hidden;
    }
    .hero-illustration {
        position: relative;
        display: flex;
        align-items: center;
        justify-content: center;
        height: 300px;
    }
    .min-vh-50 { min-height: 50vh; }
    .service-card { transition: transform 0.2s ease, box-shadow 0.2s ease; }
    .service-card:hover { transform: translateY(-5px); box-shadow: 0 0.5rem 1.5rem rgba(0,0,0,0.15) !important; }
    .cta-section {
        background: linear-gradient(135deg, var(--primary) 0%, var(--secondary) 100%);
    }
</style>
INDEXEOF
echo "Deployed Index.cshtml"

echo "Restarting service..."
systemctl restart scholarrescue
systemctl status scholarrescue --no-pager -l

echo ""
echo "=== Frontend Fix Deployed ==="