import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { disciplinaryApi, extractErrorMessage } from '../api/disciplinaryApi';
import { disciplinaryQueryKeys } from '../api/disciplinaryQueryKeys';
import {
  GetDisciplinaryCasesQuery,
  CreateDisciplinaryCaseRequest,
  OpenCaseRequest,
  StartInvestigationRequest,
  SubmitCaseForDecisionRequest,
  RecordDecisionRequest,
  ApproveDecisionRequest,
  RejectDecisionRequest,
  AddOccurrenceRequest,
  AddEmployeeRequest,
  AddEvidenceRequest,
  ApplyMeasureRequest,
  CancelCaseRequest,
  ConcludeCaseRequest,
} from '../types';

export const useDisciplinaryCases = (params?: GetDisciplinaryCasesQuery) => {
  const queryClient = useQueryClient();

  const query = useQuery({
    queryKey: disciplinaryQueryKeys.cases.list(params),
    queryFn: () => disciplinaryApi.getDisciplinaryCases(params),
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateDisciplinaryCaseRequest) => disciplinaryApi.createDisciplinaryCase(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: disciplinaryQueryKeys.cases.all });
    },
  });

  return {
    ...query,
    cases: query.data?.items || [],
    pagination: {
      pageNumber: query.data?.pageNumber || 1,
      pageSize: query.data?.pageSize || 10,
      totalCount: query.data?.totalCount || 0,
      totalPages: query.data?.totalPages || 0,
      hasPreviousPage: query.data?.hasPreviousPage || false,
      hasNextPage: query.data?.hasNextPage || false,
    },
    createCase: createMutation,
    extractErrorMessage,
  };
};

export const useDisciplinaryCaseDetail = (id: string | undefined) => {
  const queryClient = useQueryClient();

  const detailQuery = useQuery({
    queryKey: id ? disciplinaryQueryKeys.cases.detail(id) : ['disciplinary-cases', 'detail', 'disabled'],
    queryFn: () => (id ? disciplinaryApi.getDisciplinaryCaseById(id) : Promise.reject('ID não informado')),
    enabled: !!id,
  });

  const invalidateDetail = () => {
    queryClient.invalidateQueries({ queryKey: disciplinaryQueryKeys.cases.all });
    if (id) {
      queryClient.invalidateQueries({ queryKey: disciplinaryQueryKeys.cases.detail(id) });
    }
  };

  const openMutation = useMutation({
    mutationFn: (data?: OpenCaseRequest) => (id ? disciplinaryApi.openCase(id, data) : Promise.reject()),
    onSuccess: invalidateDetail,
  });

  const startInvestigationMutation = useMutation({
    mutationFn: (data?: StartInvestigationRequest) => (id ? disciplinaryApi.startInvestigation(id, data) : Promise.reject()),
    onSuccess: invalidateDetail,
  });

  const submitForDecisionMutation = useMutation({
    mutationFn: (data?: SubmitCaseForDecisionRequest) => (id ? disciplinaryApi.submitForDecision(id, data) : Promise.reject()),
    onSuccess: invalidateDetail,
  });

  const recordDecisionMutation = useMutation({
    mutationFn: (data: RecordDecisionRequest) => (id ? disciplinaryApi.recordDecision(id, data) : Promise.reject()),
    onSuccess: invalidateDetail,
  });

  const approveDecisionMutation = useMutation({
    mutationFn: (data?: ApproveDecisionRequest) => (id ? disciplinaryApi.approveDecision(id, data) : Promise.reject()),
    onSuccess: invalidateDetail,
  });

  const rejectDecisionMutation = useMutation({
    mutationFn: (data: RejectDecisionRequest) => (id ? disciplinaryApi.rejectDecision(id, data) : Promise.reject()),
    onSuccess: invalidateDetail,
  });

  const addOccurrenceMutation = useMutation({
    mutationFn: (data: AddOccurrenceRequest) => (id ? disciplinaryApi.addOccurrence(id, data) : Promise.reject()),
    onSuccess: invalidateDetail,
  });

  const addEmployeeMutation = useMutation({
    mutationFn: (data: AddEmployeeRequest) => (id ? disciplinaryApi.addEmployee(id, data) : Promise.reject()),
    onSuccess: invalidateDetail,
  });

  const addEvidenceMutation = useMutation({
    mutationFn: (data: AddEvidenceRequest) => (id ? disciplinaryApi.addEvidence(id, data) : Promise.reject()),
    onSuccess: invalidateDetail,
  });

  const applyMeasureMutation = useMutation({
    mutationFn: (data: ApplyMeasureRequest) => (id ? disciplinaryApi.applyMeasure(id, data) : Promise.reject()),
    onSuccess: invalidateDetail,
  });

  const cancelCaseMutation = useMutation({
    mutationFn: (data: CancelCaseRequest) => (id ? disciplinaryApi.cancelCase(id, data) : Promise.reject()),
    onSuccess: invalidateDetail,
  });

  const concludeCaseMutation = useMutation({
    mutationFn: (data: ConcludeCaseRequest) => (id ? disciplinaryApi.concludeCase(id, data) : Promise.reject()),
    onSuccess: invalidateDetail,
  });

  return {
    ...detailQuery,
    caseDetail: detailQuery.data,
    openCase: openMutation,
    startInvestigation: startInvestigationMutation,
    submitForDecision: submitForDecisionMutation,
    recordDecision: recordDecisionMutation,
    approveDecision: approveDecisionMutation,
    rejectDecision: rejectDecisionMutation,
    addOccurrence: addOccurrenceMutation,
    addEmployee: addEmployeeMutation,
    addEvidence: addEvidenceMutation,
    applyMeasure: applyMeasureMutation,
    cancelCase: cancelCaseMutation,
    concludeCase: concludeCaseMutation,
    extractErrorMessage,
  };
};
