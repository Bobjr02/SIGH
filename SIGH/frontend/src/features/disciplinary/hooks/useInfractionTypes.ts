import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { disciplinaryApi, extractErrorMessage } from '../api/disciplinaryApi';
import { disciplinaryQueryKeys } from '../api/disciplinaryQueryKeys';
import {
  GetInfractionTypesQuery,
  CreateInfractionTypeRequest,
  UpdateInfractionTypeRequest,
} from '../types';

export const useInfractionTypes = (params?: GetInfractionTypesQuery) => {
  const queryClient = useQueryClient();

  const query = useQuery({
    queryKey: disciplinaryQueryKeys.infractionTypes.list(params),
    queryFn: () => disciplinaryApi.getInfractionTypes(params),
  });

  const invalidateAll = () => {
    queryClient.invalidateQueries({ queryKey: disciplinaryQueryKeys.infractionTypes.all });
  };

  const createMutation = useMutation({
    mutationFn: (data: CreateInfractionTypeRequest) => disciplinaryApi.createInfractionType(data),
    onSuccess: invalidateAll,
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateInfractionTypeRequest }) =>
      disciplinaryApi.updateInfractionType(id, data),
    onSuccess: invalidateAll,
  });

  const activateMutation = useMutation({
    mutationFn: (id: string) => disciplinaryApi.activateInfractionType(id),
    onSuccess: invalidateAll,
  });

  const deactivateMutation = useMutation({
    mutationFn: (id: string) => disciplinaryApi.deactivateInfractionType(id),
    onSuccess: invalidateAll,
  });

  return {
    ...query,
    infractionTypes: query.data?.items || [],
    pagination: {
      pageNumber: query.data?.pageNumber || 1,
      pageSize: query.data?.pageSize || 10,
      totalCount: query.data?.totalCount || 0,
      totalPages: query.data?.totalPages || 0,
      hasPreviousPage: query.data?.hasPreviousPage || false,
      hasNextPage: query.data?.hasNextPage || false,
    },
    createInfractionType: createMutation,
    updateInfractionType: updateMutation,
    activateInfractionType: activateMutation,
    deactivateInfractionType: deactivateMutation,
    extractErrorMessage,
  };
};

export const useInfractionTypeDetail = (id: string | undefined) => {
  return useQuery({
    queryKey: id ? disciplinaryQueryKeys.infractionTypes.detail(id) : ['infraction-types', 'detail', 'disabled'],
    queryFn: () => (id ? disciplinaryApi.getInfractionTypeById(id) : Promise.reject('ID não informado')),
    enabled: !!id,
  });
};
