import React from 'react';
import { Route } from 'react-router-dom';
import { InfractionTypesPage } from '../pages/InfractionTypesPage';
import { DisciplinaryCasesListPage } from '../pages/DisciplinaryCasesListPage';
import { CreateDisciplinaryCasePage } from '../pages/CreateDisciplinaryCasePage';
import { DisciplinaryCaseDetailPage } from '../pages/DisciplinaryCaseDetailPage';

export const disciplinaryRoutes = [
  <Route key="infraction-types" path="/disciplinary/infraction-types" element={<InfractionTypesPage />} />,
  <Route key="cases-list" path="/disciplinary/cases" element={<DisciplinaryCasesListPage />} />,
  <Route key="case-create" path="/disciplinary/cases/new" element={<CreateDisciplinaryCasePage />} />,
  <Route key="case-detail" path="/disciplinary/cases/:id" element={<DisciplinaryCaseDetailPage />} />,
];
