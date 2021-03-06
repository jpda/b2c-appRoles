/* tslint:disable */

/**
 * This file was automatically generated by "Swaxios".
 * It should not be modified by hand.
 */

import {AxiosInstance, AxiosRequestConfig} from 'axios';
import {UserApplication} from '../../interfaces/';

export class ServicePrincipalsService {
  private readonly apiClient: AxiosInstance;

  constructor(apiClient: AxiosInstance) {
    this.apiClient = apiClient;
  }

  getByResourceId = async (resourceId: string): Promise<UserApplication> => {
    const config: AxiosRequestConfig = {
      method: 'get',
      url: `/v1.0/servicePrincipals/${resourceId}`,
    };
    const response = await this.apiClient.request<UserApplication>(config);
    return response.data;
  };

  deleteByResourceId = async (
    resourceId: string,
    appRoleAssignmentId: string
  ): Promise<void> => {
    const config: AxiosRequestConfig = {
      method: 'delete',
      url: `/v1.0/servicePrincipals/${resourceId}/appRoleAssignedTo/${appRoleAssignmentId}`,
    };
    await this.apiClient.request(config);
  };
}
