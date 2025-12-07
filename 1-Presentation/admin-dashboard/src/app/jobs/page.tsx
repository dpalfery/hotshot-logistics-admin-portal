import { Suspense } from 'react';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { JobsManagement } from '@/components/jobs/JobsManagement';

export default function JobsPage() {
  return (
    <DashboardLayout>
      <Suspense fallback={<div>Loading...</div>}>
        <JobsManagement />
      </Suspense>
    </DashboardLayout>
  );
}