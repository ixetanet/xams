import MainLayout from "@/components/MainLayout";

import React from "react";

const PrivacyPolicy = () => {
  return (
    <MainLayout>
      <div className="w-full flex justify-center py-6 px-2 lg:px-0">
        <div className="max-w-3xl flex flex-col gap-4">
          <h1>Privacy Policy</h1>

          <p>
            <strong>Effective Date:</strong> June 14, 2024
          </p>

          <p>
            <strong>1. Introduction</strong>
          </p>
          <p>
            Welcome to Xams. We respect your privacy and are committed to
            protecting any personal information you may provide while using our
            website. This Privacy Policy explains how we collect, use, disclose,
            and protect your information.
          </p>

          <p>
            <strong>2. Information We Collect</strong>
          </p>
          <p>
            We do not require users to log in or authenticate to access our
            website. However, we do use Google Analytics to collect information
            about how our site is used. The information collected includes:
          </p>
          <ul>
            <li>
              <strong>Device Information:</strong> Type of device, operating
              system, and browser used to access the website.
            </li>
            <li>
              <strong>Usage Information:</strong> Pages visited, time spent on
              pages, and referring URLs.
            </li>
            <li>
              <strong>Location Information:</strong> Geographic location based
              on IP address.
            </li>
          </ul>

          <p>
            <strong>3. How We Use Your Information</strong>
          </p>
          <p>The information collected through Google Analytics is used to:</p>
          <ul>
            <li>Understand how users interact with our website.</li>
            <li>Improve our website&apos;s content and functionality.</li>
            <li>Analyze trends and gather demographic information.</li>
          </ul>

          <p>
            <strong>4. Disclosure of Information</strong>
          </p>
          <p>
            We do not sell, trade, or otherwise transfer your personally
            identifiable information to outside parties. The data collected by
            Google Analytics is subject to Google&apos;s privacy policies.
          </p>

          <p>
            <strong>5. Data Security</strong>
          </p>
          <p>
            We implement appropriate security measures to protect the
            information collected through our website. However, please be aware
            that no method of internet transmission or electronic storage is
            100% secure.
          </p>

          <p>
            <strong>6. Third-Party Services</strong>
          </p>
          <p>
            Our website may contain links to third-party websites. We are not
            responsible for the privacy practices or content of these
            third-party sites.
          </p>

          <p>
            <strong>7. Changes to This Privacy Policy</strong>
          </p>
          <p>
            We may update this Privacy Policy from time to time. Any changes
            will be posted on this page with an updated effective date.
          </p>

          <p>
            <strong>8. Contact Us</strong>
          </p>
          <p>
            If you have any questions about this Privacy Policy, please contact
            us at [Your Contact Information].
          </p>

          <p>
            <strong>Effective Date:</strong> June 14, 2024
          </p>

          <p>Thank you for visiting Xams!</p>
        </div>
      </div>
    </MainLayout>
  );
};

export default PrivacyPolicy;
